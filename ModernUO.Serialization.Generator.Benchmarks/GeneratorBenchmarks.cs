/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GeneratorBenchmarks.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModernUO.Serialization.Generator;
using ModernUO.Serialization.Generator.Tests.Helpers;

namespace ModernUO.Serialization.Generator.Benchmarks;

/// <summary>
/// Measures the generator over a synthetic corpus: a cold full run, and the incremental
/// re-run cost after editing a single file - the operation an IDE performs constantly.
/// A correctly incremental pipeline keeps the re-run near the cost of one class; a broken
/// one re-runs the whole corpus.
/// </summary>
[MemoryDiagnoser]
public class GeneratorBenchmarks
{
    [Params(150)]
    public int ClassCount { get; set; }

    private CSharpCompilation _compilation = null!;
    private ImmutableArray<AdditionalText> _additionalTexts;
    private List<MetadataReference> _references = null!;

    private GeneratorDriver _warmDriver = null!;
    private CSharpCompilation _warmCompilation = null!;
    private SyntaxTree _editTarget = null!;
    private bool _editToggle;

    [GlobalSetup]
    public void Setup()
    {
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        _references = trustedAssemblies
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat([MetadataReference.CreateFromFile(typeof(SerializationGeneratorAttribute).Assembly.Location)])
            .ToList();

        var trees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(SourceGeneratorTestHelper.ServerStubs)
        };

        var additionalTexts = ImmutableArray.CreateBuilder<AdditionalText>();

        for (var i = 0; i < ClassCount; i++)
        {
            trees.Add(CSharpSyntaxTree.ParseText(BuildClassSource(i)));
            additionalTexts.Add(
                new InMemoryAdditionalText($"Server.TestContent.BenchItem{i}.v0.json", BuildMigrationJson(i))
            );
        }

        _additionalTexts = additionalTexts.ToImmutable();
        _compilation = CSharpCompilation.Create(
            "BenchmarkAssembly",
            trees,
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Warm state for the incremental benchmark: one full run, then per-invocation edits.
        _warmDriver = CSharpGeneratorDriver
            .Create(new EntitySerializationGenerator())
            .AddAdditionalTexts(_additionalTexts)
            .RunGenerators(_compilation);
        _warmCompilation = _compilation;
        _editTarget = _warmCompilation.SyntaxTrees.Last();

        // Guard against measuring a silently failing generator.
        var result = _warmDriver.GetRunResult().Results[0];
        if (result.GeneratedSources.Length != ClassCount)
        {
            throw new InvalidOperationException(
                $"Expected {ClassCount} generated sources but got {result.GeneratedSources.Length}. " +
                $"Diagnostics: {string.Join("; ", result.Diagnostics.Take(3))}"
            );
        }
    }

    [Benchmark]
    public GeneratorDriver ColdFullRun() =>
        CSharpGeneratorDriver
            .Create(new EntitySerializationGenerator())
            .AddAdditionalTexts(_additionalTexts)
            .RunGenerators(_compilation);

    [Benchmark]
    public GeneratorDriver WarmRerunNoChange()
    {
        _warmDriver = _warmDriver.RunGenerators(_warmCompilation);
        return _warmDriver;
    }

    [Benchmark]
    public GeneratorDriver WarmRerunAfterSingleEdit()
    {
        // Toggle one file between two whitespace variants so every invocation is a real edit.
        _editToggle = !_editToggle;
        var text = _editTarget.GetText().ToString();
        var edited = _editToggle ? text + "\n// edit\n" : text.Replace("\n// edit\n", "");
        var newTree = CSharpSyntaxTree.ParseText(edited);

        _warmCompilation = _warmCompilation.ReplaceSyntaxTree(_editTarget, newTree);
        _editTarget = newTree;

        _warmDriver = _warmDriver.RunGenerators(_warmCompilation);
        return _warmDriver;
    }

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
}
