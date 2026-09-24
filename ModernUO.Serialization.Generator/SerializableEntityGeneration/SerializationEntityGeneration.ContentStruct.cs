/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationEntityGeneration.ContentStruct.cs                  *
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
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    public static void GenerateMigrationContentStruct(
        this StringBuilder source,
        string indent,
        SerializableMetadata migration,
        string classDisplayString
    )
    {
        source.AppendLine($"{indent}ref struct V{migration.Version}Content");
        source.AppendLine($"{indent}{{");
        var properties = migration.Properties ?? ImmutableArray<SerializableProperty>.Empty;

        foreach (var serializableProperty in properties)
        {
            SerializableMigrationRulesEngine.Rules[serializableProperty.Rule].GenerateMigrationProperty(
                source, $"{indent}    ", serializableProperty
            );
        }

        var innerIndent = $"{indent}        ";

        // The stream was written by the live generator of that version, so partition the flags
        // exactly like the live SaveFlag enums: int up to 32 flags, ulong up to 64, then one more
        // ulong enum per 64 flags. Readonly fields never reach the schema, so none are skipped here.
        var saveFlagCount = properties.Count(p => p.UsesSaveFlag == true);
        var saveFlagUseUlong = saveFlagCount > 32;
        var saveFlagEnumCount = saveFlagCount == 0 ? 0 : saveFlagCount <= 64 ? 1 : (saveFlagCount + 63) / 64;
        var bitsPerEnum = saveFlagUseUlong ? 64 : 32;

        var flagIndex = 0;
        foreach (var property in properties)
        {
            if (property.UsesSaveFlag != true)
            {
                continue;
            }

            var bitIndex = flagIndex % bitsPerEnum;
            if (bitIndex == 0)
            {
                if (flagIndex > 0)
                {
                    source.GenerateEnumEnd($"{indent}    ");
                }

                source.AppendLine();
                source.GenerateEnumStart(
                    GetContentSaveFlagEnumName(migration.Version, flagIndex / bitsPerEnum),
                    $"{indent}    ",
                    true,
                    Accessibility.Private,
                    saveFlagUseUlong ? "ulong" : null
                );

                source.GenerateContentSaveFlagValue(innerIndent, saveFlagUseUlong, "None", -1);
            }

            source.GenerateContentSaveFlagValue(innerIndent, saveFlagUseUlong, property.Name, bitIndex);
            flagIndex++;
        }

        if (flagIndex > 0)
        {
            source.GenerateEnumEnd($"{indent}    ");
        }

        source.AppendLine($"{indent}    internal V{migration.Version}Content(Server.IGenericReader reader, {classDisplayString} entity)");
        source.AppendLine($"{indent}    {{");

        // The writer emits every flag enum before any field.
        for (var i = 0; i < saveFlagEnumCount; i++)
        {
            source.AppendLine(
                $"{innerIndent}var {GetContentSaveFlagVariableName(i)} = reader.ReadEnum<{GetContentSaveFlagEnumName(migration.Version, i)}>();"
            );
        }

        flagIndex = 0;
        foreach (var property in properties)
        {
            if (property.UsesSaveFlag == true)
            {
                var enumIndex = flagIndex++ / bitsPerEnum;
                var flagTest =
                    $"({GetContentSaveFlagVariableName(enumIndex)} & {GetContentSaveFlagEnumName(migration.Version, enumIndex)}.{property.Name}) != 0";

                source.AppendLine();
                // Special case
                if (property.Type == "bool")
                {
                    source.AppendLine($"{innerIndent}{property.Name} = {flagTest};");
                }
                else
                {
                    source.AppendLine($"{innerIndent}if ({flagTest})\n{innerIndent}{{");

                    SerializableMigrationRulesEngine.Rules[property.Rule].GenerateDeserializationMethod(
                        source,
                        $"{innerIndent}    ",
                        property,
                        "entity",
                        true
                    );

                    source.AppendLine($"{innerIndent}}}\n{innerIndent}else\n{innerIndent}{{");
                    source.AppendLine($"{innerIndent}    {property.Name} = default;");
                    source.AppendLine($"{innerIndent}}}");
                }
            }
            else
            {
                SerializableMigrationRulesEngine.Rules[property.Rule].GenerateDeserializationMethod(
                    source,
                    innerIndent,
                    property,
                    "entity",
                    true
                );
            }
        }

        source.AppendLine($"{indent}    }}");

        source.AppendLine($"{indent}}}");
    }

    private static string GetContentSaveFlagEnumName(int version, int enumIndex) =>
        enumIndex == 0 ? $"V{version}SaveFlag" : $"V{version}SaveFlag{enumIndex + 1}";

    private static string GetContentSaveFlagVariableName(int enumIndex) =>
        enumIndex == 0 ? "saveFlags" : $"saveFlags{enumIndex + 1}";

    private static void GenerateContentSaveFlagValue(
        this StringBuilder source,
        string indent,
        bool useUlong,
        string name,
        int bitIndex
    )
    {
        if (useUlong)
        {
            source.GenerateEnumValueLong(indent, true, name, bitIndex);
        }
        else
        {
            source.GenerateEnumValue(indent, true, name, bitIndex);
        }
    }
}
