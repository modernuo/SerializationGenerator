/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.DeserializeMethod.cs               *
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
using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    extension(StringBuilder source)
    {
        public void GenerateDeserializeMethod(
            SerializationModel model,
            string indent,
            bool isOverride,
            int version,
            bool encodedVersion,
            ImmutableArray<SerializableMetadata> migrations,
            ImmutableArray<SerializableProperty> fields,
            string markDirtyMethod,
            string parentReference,
            SortedDictionary<int, SaveFlagModel> serializableFieldSaveFlagMethodsDictionary,
            Dictionary<int, (int EnumIndex, int BitIndex)> saveFlagMapping,
            bool saveFlagUseUlong,
            int saveFlagEnumCount,
            bool isVirtual = true
        )
        {
            source.GenerateMethodStart(
                indent,
                "Deserialize",
                Accessibility.Public,
                isOverride,
                "void",
                ImmutableArray.Create(("Server.IGenericReader", "reader")),
                isVirtual
            );

            var bodyIndent = $"{indent}    ";
            var innerIndent = $"{bodyIndent}    ";

            if (isOverride)
            {
                source.AppendLine($"{bodyIndent}base.Deserialize(reader);");
                source.AppendLine();
            }

            var afterDeserialization = model.AfterDeserialization;

            // Version
            source.AppendLine($"{bodyIndent}var version = reader.{(encodedVersion ? "ReadEncodedInt" : "ReadInt")}();");

            if (version > 0)
            {
                var nextVersion = 0;

                for (var i = 0; i < migrations.Length; i++)
                {
                    var migrationVersion = migrations[i].Version;
                    if (migrationVersion == nextVersion)
                    {
                        nextVersion++;
                    }

                    source.AppendLine();
                    source.AppendLine($"{bodyIndent}if (version == {migrationVersion})");
                    source.AppendLine($"{bodyIndent}{{");
                    source.AppendLine($"{bodyIndent}    MigrateFrom(new V{migrationVersion}Content(reader, this));");
                    if (markDirtyMethod != null)
                    {
                        source.AppendLine($"{bodyIndent}    {markDirtyMethod};");
                    }
                    source.GenerateAfterDeserialization($"{bodyIndent}    ", afterDeserialization);
                    source.AppendLine($"{bodyIndent}    return;");
                    source.AppendLine($"{bodyIndent}}}");
                }

                if (nextVersion < version)
                {
                    source.AppendLine();
                    source.AppendLine($"{bodyIndent}if (version < SerializationVersion)");
                    source.AppendLine($"{bodyIndent}{{");
                    source.AppendLine($"{bodyIndent}    Deserialize(reader, version);");
                    if (markDirtyMethod != null)
                    {
                        source.AppendLine($"{bodyIndent}    {markDirtyMethod};");
                    }
                    source.GenerateAfterDeserialization($"{bodyIndent}    ", afterDeserialization);
                    source.AppendLine($"{bodyIndent}    return;");
                    source.AppendLine($"{bodyIndent}}}");
                }
            }

            if (saveFlagMapping.Count > 0)
            {
                source.AppendLine();
                // Read all save flag enums
                for (var i = 0; i < saveFlagEnumCount; i++)
                {
                    var enumName = i == 0 ? "SaveFlag" : $"SaveFlag{i + 1}";
                    var varName = i == 0 ? "saveFlags" : $"saveFlags{i + 1}";
                    source.AppendLine($"{bodyIndent}var {varName} = reader.ReadEnum<{enumName}>();");
                }
            }

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                // Skip readonly fields - they cannot be assigned outside the constructor
                if (field.IsReadOnly)
                {
                    continue;
                }

                var rule = SerializableMigrationRulesEngine.Rules[field.Rule];

                if (serializableFieldSaveFlagMethodsDictionary.TryGetValue(
                        field.Order,
                        out var serializableFieldSaveFlagMethods
                    ) && saveFlagMapping.TryGetValue(field.Order, out var mapping))
                {
                    var enumName = mapping.EnumIndex == 0 ? "SaveFlag" : $"SaveFlag{mapping.EnumIndex + 1}";
                    var varName = mapping.EnumIndex == 0 ? "saveFlags" : $"saveFlags{mapping.EnumIndex + 1}";

                    source.AppendLine();
                    // Special case
                    if (field.Type == "bool")
                    {
                        source.AppendLine($"{bodyIndent}{field.FieldName} = ({varName} & {enumName}.{field.Name}) != 0;");
                    }
                    else
                    {
                        source.AppendLine($"{bodyIndent}if (({varName} & {enumName}.{field.Name}) != 0)\n{bodyIndent}{{");
                        rule.GenerateDeserializationMethod(
                            source,
                            innerIndent,
                            field,
                            parentReference
                        );
                        (rule as IPostDeserializeMethod)?.PostDeserializeMethod(
                            source,
                            innerIndent,
                            field,
                            model
                        );

                        if (serializableFieldSaveFlagMethods.DefaultName != null)
                        {
                            source.AppendLine($"{bodyIndent}}}\n{bodyIndent}else\n{bodyIndent}{{");
                            source.AppendLine(
                                $"{bodyIndent}    {field.FieldName} = {serializableFieldSaveFlagMethods.DefaultName}();"
                            );
                        }

                        source.AppendLine($"{bodyIndent}}}");
                    }
                }
                else
                {
                    source.AppendLine();
                    rule.GenerateDeserializationMethod(
                        source,
                        bodyIndent,
                        field,
                        parentReference
                    );
                    (rule as IPostDeserializeMethod)?.PostDeserializeMethod(
                        source,
                        bodyIndent,
                        field,
                        model
                    );
                }
            }

            source.GenerateAfterDeserialization($"{bodyIndent}", afterDeserialization);
            source.GenerateMethodEnd(indent);
        }

        private void GenerateAfterDeserialization(
            string indent, EquatableArray<AfterDeserializeModel> afterDeserialization
        )
        {
            foreach (var method in afterDeserialization)
            {
                if (method.Synchronous)
                {
                    source.AppendLine($"{indent}{method.MethodName}();");
                }
                else
                {
                    source.AppendLine($"{indent}Server.Timer.DelayCall({method.MethodName});");
                }
            }
        }
    }
}
