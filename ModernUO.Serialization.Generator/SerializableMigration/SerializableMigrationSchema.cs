/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigrationSchema.cs                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SerializationGenerator;

namespace SerializableMigration;

public static class SerializableMigrationSchema
{
    private static JsonSerializerOptions defaultSerializerOptions;

    public static JsonSerializerOptions GetJsonSerializerOptions() =>
        defaultSerializerOptions ??= new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

    // <ClassName>.v*.json
    public static readonly Regex MigrationFileRegex = new(@"^(\S+)\.[vV](\d+)\.json$", RegexOptions.Compiled);

    public static bool MatchMigrationFilename(string fileName, out string className, out int version)
    {
        var regexMatch = MigrationFileRegex.Match(fileName);
        if (!regexMatch.Success)
        {
            className = "";
            version = -1;
            return false;
        }

        className = regexMatch.Captures[0].Value;
        return int.TryParse(regexMatch.Captures[1].Value, out version);
    }

    // Used during schema migration which is not incremental
    private static ImmutableDictionary<string, ImmutableDictionary<int, AdditionalText>>? _cache = null;

    public static ImmutableDictionary<int, AdditionalText> GetMigrations(INamedTypeSymbol classSymbol, string migrationPath)
    {
        GetMigrations(migrationPath);

        return _cache!.TryGetValue(classSymbol.ToDisplayString(), out var additionalTexts)
            ? additionalTexts
            : ImmutableDictionary<int, AdditionalText>.Empty;
    }

    private static void GetMigrations(string migrationPath)
    {
        if (_cache != null)
        {
            return;
        }

        var migrationFilesByClass = new Dictionary<string, List<(int, string)>>();

        var migrationFiles = Directory.GetFiles(migrationPath, "*.v*.json");

        foreach (var file in migrationFiles)
        {
            var fileName = Path.GetFileName(file);
            if (!MatchMigrationFilename(fileName, out var className, out var version))
            {
                continue;
            }

            if (migrationFilesByClass.TryGetValue(className, out var fileList))
            {
                fileList.Add((version, file));
            }
            else
            {
                migrationFilesByClass[className] = new List<(int, string)> { (version, file) };
            }
        }

        var cacheBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<int, AdditionalText>>();

        foreach (var (className, fileList) in migrationFilesByClass)
        {
            var additionalTextBuilder = ImmutableDictionary.CreateBuilder<int, AdditionalText>();
            foreach (var (version, file) in fileList)
            {
                var sourceText = SourceText.From(File.ReadAllText(file), Encoding.UTF8);
                var additionalText = new AdditionalTextSchemaFile(file, sourceText);
                additionalTextBuilder[version] = additionalText;
            }

            cacheBuilder[className] = additionalTextBuilder.ToImmutable();
        }

        _cache = cacheBuilder.ToImmutable();
    }
}
