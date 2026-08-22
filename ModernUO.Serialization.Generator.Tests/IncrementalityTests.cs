using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Proves the pipeline actually caches: after an edit, only affected classes may re-run the
/// source-output stage, and untouched migration files are not re-parsed. Uses Roslyn's
/// incremental step tracking, so "cached" means the callback did not execute at all.
/// </summary>
public class IncrementalityTests
{
    private const string ClassA = """
        using System;
        using ModernUO.Serialization;
        using Server;

        namespace Server.TestContent
        {
            [SerializationGenerator(1)]
            public partial class AlphaItem : ISerializable
            {
                [SerializableField(0)]
                private string _name;

                public DateTime Created { get; set; }
                public Serial Serial { get; }
                public bool Deleted => false;
                public void Delete() { }

                private void MigrateFrom(V0Content content)
                {
                    _name = content.Name;
                }
            }
        }
        """;

    private const string ClassB = """
        using System;
        using ModernUO.Serialization;
        using Server;

        namespace Server.TestContent
        {
            [SerializationGenerator(0)]
            public partial class BravoItem : ISerializable
            {
                [SerializableField(0)]
                private int _charges;

                public DateTime Created { get; set; }
                public Serial Serial { get; }
                public bool Deleted => false;
                public void Delete() { }
            }
        }
        """;

    private const string UnrelatedClass = """
        namespace Server.TestContent
        {
            public class Bystander
            {
                public int Value { get; set; }
            }
        }
        """;

    private const string AlphaMigrationJson = """
        {
            "version": 0,
            "type": "Server.TestContent.AlphaItem",
            "properties": [
                {
                    "name": "Name",
                    "type": "string",
                    "rule": "PrimitiveTypeMigrationRule"
                }
            ]
        }
        """;

    private static (GeneratorDriver Driver, CSharpCompilation Compilation, Dictionary<string, AdditionalText> Texts)
        CreateTrackedRun(
            Dictionary<string, string> sources,
            IEnumerable<(string fileName, string content)> additionalTexts
        )
    {
        var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(SourceGeneratorTestHelper.ServerStubs) };
        foreach (var (path, text) in sources)
        {
            trees.Add(CSharpSyntaxTree.ParseText(text, path: path));
        }

        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        var references = trustedAssemblies
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat([MetadataReference.CreateFromFile(typeof(SerializationGeneratorAttribute).Assembly.Location)])
            .ToList();

        var compilation = CSharpCompilation.Create(
            "IncrementalityAssembly",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var texts = additionalTexts.ToDictionary(
            t => t.fileName,
            t => (AdditionalText)new InMemoryAdditionalText(t.fileName, t.content)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new EntitySerializationGenerator().AsSourceGenerator()],
            additionalTexts: texts.Values,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

        driver = driver.RunGenerators(compilation);
        return (driver, compilation, texts);
    }

    private static CSharpCompilation ReplaceTree(CSharpCompilation compilation, string path, string newText)
    {
        var oldTree = compilation.SyntaxTrees.Single(t => t.FilePath == path);
        return compilation.ReplaceSyntaxTree(oldTree, CSharpSyntaxTree.ParseText(newText, path: path));
    }

    private static (int Executed, int Cached) CountSourceOutputRuns(GeneratorDriver driver)
    {
        var executed = 0;
        var cached = 0;

        foreach (var (_, steps) in driver.GetRunResult().Results[0].TrackedOutputSteps)
        {
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
        }

        return (executed, cached);
    }

    [Fact]
    public void EditingOneClass_DoesNotRegenerateOthers()
    {
        var sources = new Dictionary<string, string> { ["A.cs"] = ClassA, ["B.cs"] = ClassB };
        var (driver, compilation, _) = CreateTrackedRun(
            sources,
            [("Server.TestContent.AlphaItem.v0.json", AlphaMigrationJson)]
        );

        var edited = ReplaceTree(compilation, "B.cs", ClassB + "\n// edit\n");
        driver = driver.RunGenerators(edited);

        var (executed, cached) = CountSourceOutputRuns(driver);

        Assert.True(cached >= 1, "The untouched class must be served from cache.");
        Assert.True(
            executed <= 1,
            $"Only the edited class may re-run the output stage, but {executed} outputs ran."
        );
    }

    [Fact]
    public void EditingAnUnrelatedFile_RegeneratesNothing()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = ClassA, ["B.cs"] = ClassB, ["C.cs"] = UnrelatedClass
        };
        var (driver, compilation, _) = CreateTrackedRun(
            sources,
            [("Server.TestContent.AlphaItem.v0.json", AlphaMigrationJson)]
        );

        var edited = ReplaceTree(compilation, "C.cs", UnrelatedClass + "\n// edit\n");
        driver = driver.RunGenerators(edited);

        var (executed, _) = CountSourceOutputRuns(driver);

        Assert.True(
            executed == 0,
            $"An edit to a non-serializable file must not re-run any output stage, but {executed} outputs ran."
        );
    }

    [Fact]
    public void EditingACodeFile_DoesNotReparseMigrationFiles()
    {
        var sources = new Dictionary<string, string> { ["A.cs"] = ClassA, ["B.cs"] = ClassB };
        var (driver, compilation, _) = CreateTrackedRun(
            sources,
            [("Server.TestContent.AlphaItem.v0.json", AlphaMigrationJson)]
        );

        var edited = ReplaceTree(compilation, "B.cs", ClassB + "\n// edit\n");
        driver = driver.RunGenerators(edited);

        var result = driver.GetRunResult().Results[0];
        Assert.True(
            result.TrackedSteps.TryGetValue("migrationFiles", out var parseSteps),
            "The migration parse node must be tracked as 'migrationFiles'."
        );

        var reparsed = parseSteps
            .SelectMany(s => s.Outputs)
            .Count(o => o.Reason is not (IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));

        Assert.True(reparsed == 0, $"No migration file changed, but {reparsed} parse outputs ran.");
    }

    // The payoff case: editing logic inside a serializable class - a method body, a comment -
    // produces an equal model, so nothing regenerates even though the class itself changed.
    [Fact]
    public void EditingNonSerializationCode_InASerializableClass_RegeneratesNothing()
    {
        var sources = new Dictionary<string, string> { ["A.cs"] = ClassA, ["B.cs"] = ClassB };
        var (driver, compilation, _) = CreateTrackedRun(
            sources,
            [("Server.TestContent.AlphaItem.v0.json", AlphaMigrationJson)]
        );

        // Add a method that has nothing to do with serialization.
        var edited = ReplaceTree(
            compilation,
            "B.cs",
            ClassB.Replace(
                "public void Delete() { }",
                "public void Delete() { }\n\n        public int ComputeDamage(int roll) => roll * 2 + _charges;"
            )
        );
        driver = driver.RunGenerators(edited);

        var (executed, _) = CountSourceOutputRuns(driver);

        Assert.True(
            executed == 0,
            $"A non-serialization edit inside a serializable class must not regenerate, but {executed} outputs ran."
        );
    }

    [Fact]
    public void EditingAMigrationFile_OnlyAffectsItsClass()
    {
        var sources = new Dictionary<string, string> { ["A.cs"] = ClassA, ["B.cs"] = ClassB };
        var (driver, compilation, texts) = CreateTrackedRun(
            sources,
            [("Server.TestContent.AlphaItem.v0.json", AlphaMigrationJson)]
        );

        // Same compilation; only the additional text changes.
        var newJson = AlphaMigrationJson.Replace("\"Name\"", "\"Name\" ");
        driver = driver
            .ReplaceAdditionalText(
                texts["Server.TestContent.AlphaItem.v0.json"],
                new InMemoryAdditionalText("Server.TestContent.AlphaItem.v0.json", newJson)
            )
            .RunGenerators(compilation);

        var (executed, cached) = CountSourceOutputRuns(driver);

        Assert.True(cached >= 1, "The class without migrations must be served from cache.");
        Assert.True(
            executed <= 1,
            $"Only the class owning the edited migration may re-run, but {executed} outputs ran."
        );
    }
}
