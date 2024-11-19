/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceCodeAnalysis.cs                                           *
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace ModernUO.Serialization.SchemaGenerator;

public static class SourceCodeAnalysis
{
    public static async Task<IEnumerable<Project>> GetProjectsAsync(string solutionPath)
    {
        if (!File.Exists(solutionPath) || !solutionPath.EndsWith(".sln", StringComparison.Ordinal))
        {
            throw new FileNotFoundException($"Could not open a valid solution at location {solutionPath}");
        }

        MSBuildLocator.RegisterDefaults();

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, args) => Console.WriteLine(args.Diagnostic.Message);

        var solution = await workspace.OpenSolutionAsync(solutionPath);

        return solution
            .Projects
            .Where(project => !project.Name.EndsWith(".Tests", StringComparison.Ordinal) && project.Name != "Benchmarks")
            .Select(
                project =>
                {
                    foreach (var reference in project.AnalyzerReferences)
                    {
                        if (reference.Display == "ModernUO.Serialization.Generator")
                        {
                            project = project.RemoveAnalyzerReference(reference);
                        }
                    }

                    return project;
                });
    }
}
