/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MigrationHandler.cs                                             *
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
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ModernUO.Serialization.Generator;

namespace ModernUO.Serialization.SchemaGenerator;

public static class MigrationHandler
{
    public static ImmutableArray<AdditionalText> GetMigrations(string migrationPath)
    {
        var migrations = ImmutableArray.CreateBuilder<AdditionalText>();
        var migrationFiles = Directory.GetFiles(migrationPath, "*.v*.json");

        foreach (var file in migrationFiles)
        {
            var sourceText = SourceText.From(File.ReadAllText(file), Encoding.UTF8);
            migrations.Add(new AdditionalTextSchemaFile(file, sourceText));
        }

        return migrations.ToImmutable();
    }
}
