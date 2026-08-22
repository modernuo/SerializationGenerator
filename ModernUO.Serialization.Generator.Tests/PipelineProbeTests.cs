using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Step-level probe over a benchmark-sized corpus: reports per-node executed/cached counts
/// after a single-file edit, so a caching regression shows up as numbers, not vibes.
/// </summary>
public class PipelineProbeTests(ITestOutputHelper output)
{
    private const int ClassCount = 150;

    private static string BuildClassSource(int index)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using ModernUO.Serialization;");
        sb.AppendLine("using Server;");
        sb.AppendLine();
        sb.AppendLine("namespace Server.TestContent");
        sb.AppendLine("{");
        sb.AppendLine("    [SerializationGenerator(1)]");
        sb.AppendLine($"    public partial class BenchItem{index} : ISerializable");
        sb.AppendLine("    {");

        for (var f = 0; f < 6; f++)
        {
            sb.AppendLine($"        [SerializableField({f})]");
            sb.AppendLine($"        private {(f % 2 == 0 ? "int" : "string")} _field{f};");
            sb.AppendLine();
        }

        sb.AppendLine("        public DateTime Created { get; set; }");
        sb.AppendLine("        public Serial Serial { get; }");
        sb.AppendLine("        public bool Deleted => false;");
        sb.AppendLine("        public void Delete() { }");
        sb.AppendLine();
        sb.AppendLine("        private void MigrateFrom(V0Content content)");
        sb.AppendLine("        {");
        sb.AppendLine("            _field0 = content.Field0;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string BuildMigrationJson(int index) =>
        $$"""
        {
            "version": 0,
            "type": "Server.TestContent.BenchItem{{index}}",
            "properties": [
                {
                    "name": "Field0",
                    "type": "int",
                    "rule": "PrimitiveTypeMigrationRule"
                }
            ]
        }
        """;

    [Fact]
    public void WarmRerun_OnlyEditedClassExecutes()
    {
        var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(SourceGeneratorTestHelper.ServerStubs) };
        for (var i = 0; i < ClassCount; i++)
        {
            trees.Add(CSharpSyntaxTree.ParseText(BuildClassSource(i), path: $"BenchItem{i}.cs"));
        }

        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        var references = trustedAssemblies
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat([MetadataReference.CreateFromFile(typeof(SerializationGeneratorAttribute).Assembly.Location)])
            .ToList();

        var compilation = CSharpCompilation.Create(
            "ProbeAssembly",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var additionalTexts = new List<AdditionalText>();
        for (var i = 0; i < ClassCount; i++)
        {
            additionalTexts.Add(
                new InMemoryAdditionalText($"Server.TestContent.BenchItem{i}.v0.json", BuildMigrationJson(i))
            );
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new EntitySerializationGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

        driver = driver.RunGenerators(compilation);
        Assert.Equal(ClassCount, driver.GetRunResult().Results[0].GeneratedSources.Length);

        // Edit one file.
        var target = compilation.SyntaxTrees.Single(t => t.FilePath == "BenchItem7.cs");
        var edited = compilation.ReplaceSyntaxTree(
            target,
            CSharpSyntaxTree.ParseText(BuildClassSource(7) + "\n// edit\n", path: "BenchItem7.cs")
        );

        driver = driver.RunGenerators(edited);
        var result = driver.GetRunResult().Results[0];

        var executedOutputs = 0;
        foreach (var (name, steps) in result.TrackedSteps)
        {
            var executed = 0;
            var cached = 0;
            foreach (var step in steps)
            {
                foreach (var (_, reason) in step.Outputs)
                {
                    if (reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
                    {
                        cached++;
                    }
                    else
                    {
                        executed++;
                    }
                }
            }

            output.WriteLine($"{name}: executed={executed} cached={cached}");
        }

        foreach (var (name, steps) in result.TrackedOutputSteps)
        {
            var executed = 0;
            var cached = 0;
            foreach (var step in steps)
            {
                foreach (var (_, reason) in step.Outputs)
                {
                    if (reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
                    {
                        cached++;
                    }
                    else
                    {
                        executed++;
                    }
                }
            }

            executedOutputs += executed;
            output.WriteLine($"OUTPUT {name}: executed={executed} cached={cached}");
        }

        Assert.True(executedOutputs <= 1, $"Expected at most 1 executed output, got {executedOutputs}.");
    }
}
