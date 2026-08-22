/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Application.cs                                                  *
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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ModernUO.Serialization.Generator;
using ModernUO.Serialization.SchemaGenerator;

namespace ModernUO.Serialization.DiffTool;

/// <summary>
/// Runs the generator against every project in a solution and writes a deterministic manifest
/// of hint names and content hashes. Running it before and after a generator change and
/// diffing the manifests proves the change is output-identical across the real corpus.
/// </summary>
public static class Application
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(
                "Usage: ModernUO.Serialization.Generator.DiffTool <path to solution> <output manifest file>"
            );
            return 1;
        }

        var solutionPath = args[0];
        var outputFile = args[1];

        var stopwatch = Stopwatch.StartNew();
        var lines = new List<string>();

        foreach (var project in await SourceCodeAnalysis.GetProjectsAsync(solutionPath))
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null)
            {
                Console.WriteLine($"Skipped {project.Name}: no compilation.");
                continue;
            }

            var additionalTexts = ImmutableArray.CreateBuilder<AdditionalText>();
            foreach (var document in project.AdditionalDocuments)
            {
                if (document.FilePath != null)
                {
                    additionalTexts.Add(new DocumentAdditionalText(document.FilePath, await document.GetTextAsync()));
                }
            }

            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(new EntitySerializationGenerator())
                .AddAdditionalTexts(additionalTexts.ToImmutable());

            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult().Results[0];

            foreach (var source in result.GeneratedSources)
            {
                var normalized = Normalize(source.SourceText.ToString());
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
                lines.Add($"{project.Name}\t{source.HintName}\t{hash}");
            }

            foreach (var group in result.Diagnostics.GroupBy(d => d.Id).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                lines.Add($"{project.Name}\t#diagnostics\t{group.Key}x{group.Count()}");
            }

            Console.WriteLine($"Hashed {result.GeneratedSources.Length} sources for {project.Name}.");
        }

        lines.Sort(StringComparer.Ordinal);
        await File.WriteAllTextAsync(outputFile, string.Join("\n", lines) + "\n");

        Console.WriteLine($"Wrote {lines.Count} manifest lines to {outputFile} in {stopwatch.Elapsed.TotalSeconds:N2}s.");
        return 0;
    }

    // Version-stamped headers and line endings are normalized so manifests compare across
    // generator versions and checkout styles.
    private static string Normalize(string content) =>
        Regex.Replace(
            content.Replace("\r\n", "\n"),
            """(Version: |"ModernUO\.Serialization\.Generator", ")\d+\.\d+\.\d+\.\d+""",
            "$1{VERSION}"
        );

    private sealed class DocumentAdditionalText(string path, SourceText text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => text;
    }
}
