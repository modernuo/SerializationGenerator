/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using ModernUO.Serialization.Generator;

namespace ModernUO.Serialization.SchemaGenerator;

public static partial class Application
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Usage: ModernUO.Serialization.SchemaGenerator <path to solution>");
        }

        var solutionPath = args[0];

        Console.WriteLine($"Running Migrations for {solutionPath}");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Parallel.ForEachAsync(
            await SourceCodeAnalysis.GetProjectsAsync(solutionPath),
            async (project, cancellationToken) =>
            {
                var compilation = await project.GetCompilationAsync(cancellationToken);
                var projectFile = new FileInfo(project.FilePath!);
                var projectPath = projectFile.Directory?.FullName;
                var migrationPath = Path.Join(projectPath, "Migrations");
                Directory.CreateDirectory(migrationPath);

                var generator = new EntitySerializationGenerator(true);

                CSharpGeneratorDriver
                    .Create(generator)
                    .RunGenerators(compilation, cancellationToken);

                var options = SerializableMigrationSchema.GetJsonSerializerOptions();

                foreach (var migration in generator.Migrations.Values)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteMigration(migrationPath, migration, options, default);
                }

                Console.WriteLine($"Completed migrations for {project.Name}");
            }
        );

        stopwatch.Stop();

        Console.WriteLine("Completed in {0:N2} seconds", stopwatch.Elapsed.TotalSeconds);
    }

    private static void WriteMigration(
        string migrationPath,
        SerializableMetadata metadata,
        JsonSerializerOptions options,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(migrationPath);
        var filePath = Path.Combine(migrationPath, $"{metadata.Type}.v{metadata.Version}.json");
        var fileContents = JsonSerializer.Serialize(metadata, options);
        if (Environment.NewLine != "\n")
        {
            fileContents = NewLineRegex().Replace(fileContents, "\n");
        }
        File.WriteAllText(filePath, fileContents);
    }

    [GeneratedRegex(@"\r\n|\n\r|\n|\r")]
    private static partial Regex NewLineRegex();
}
