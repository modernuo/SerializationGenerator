/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using ModernUO.Serialization.Generator;

namespace ModernUO.Serialization.SchemaGenerator;

public static class Application
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Usage: ModernUO.Serialization.SchemaGenerator <path to solution>");
        }

        var solutionPath = args[0];

        Parallel.ForEach(
            SourceCodeAnalysis.GetCompilation(solutionPath),
            projectCompilation =>
            {
                var (project, compilation) = projectCompilation;
                if (project.Name.EndsWith(".Tests", StringComparison.Ordinal) || project.Name == "Benchmarks")
                {
                    return;
                }

                var projectFile = new FileInfo(project.FilePath!);
                var projectPath = projectFile.Directory?.FullName;
                var migrationPath = Path.Join(projectPath, "Migrations");
                Directory.CreateDirectory(migrationPath);

                var migrations = MigrationHandler.GetMigrations(migrationPath);

                CSharpGeneratorDriver
                    .Create(new EntitySerializationGenerator(migrationPath))
                    .AddAdditionalTexts(migrations)
                    .RunGenerators(compilation);
            }
        );
    }
}
