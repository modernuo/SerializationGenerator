/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ModernUO.Serialization.Generator;

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

        className = regexMatch.Groups[1].Value;
        return int.TryParse(regexMatch.Groups[2].Value, out version);
    }
}
