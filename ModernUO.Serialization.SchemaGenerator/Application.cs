/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.IO;
using ModernUO.Serialization.Generator;

namespace ModernUO.Serialization.SchemaGenerator;

public static class Application
{
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();

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

                await Parallel.ForEachAsync(generator.Migrations.Values, cancellationToken, async (migration, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    await WriteMigration(migrationPath, migration, options, ct);
                });

                Console.WriteLine($"Completed migrations for {project.Name}");
            }
        );

        stopwatch.Stop();

        Console.WriteLine("Completed in {0:N2} seconds", stopwatch.Elapsed.TotalSeconds);
    }

    private static async Task WriteMigration(
        string migrationPath,
        SerializableMetadata metadata,
        JsonSerializerOptions options,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();
        var filePath = Path.Combine(migrationPath, $"{metadata.Type}.v{metadata.Version}.json");

        await using var memoryStream = MemoryStreamManager.GetStream();
        await JsonSerializer.SerializeAsync(memoryStream, metadata, options, token);

        token.ThrowIfCancellationRequested();
        var serializedBytes = memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length);

        if (File.Exists(filePath) && new FileInfo(filePath).Length == serializedBytes.Length &&
            FileHelper.FileContentEquals(filePath, serializedBytes, token))
        {
            return;
        }

        Directory.CreateDirectory(migrationPath);
        File.WriteAllBytes(filePath, serializedBytes);
    }
}
