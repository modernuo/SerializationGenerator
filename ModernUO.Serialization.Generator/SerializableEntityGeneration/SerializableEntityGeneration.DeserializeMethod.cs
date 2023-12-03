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
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    public static void GenerateDeserializeMethod(
        this StringBuilder source,
        Compilation compilation,
        INamedTypeSymbol classSymbol,
        string indent,
        bool isOverride,
        int version,
        bool encodedVersion,
        ImmutableArray<SerializableMetadata> migrations,
        ImmutableArray<SerializableProperty> fields,
        string? markDirtyMethod,
        string? parentReference,
        SortedDictionary<int, SerializableFieldSaveFlagMethods> serializableFieldSaveFlagMethodsDictionary
    )
    {
        var genericReaderInterface = compilation.GetTypeByMetadataName(SymbolMetadata.GENERIC_READER_INTERFACE);

        source.GenerateMethodStart(
            indent,
            "Deserialize",
            Accessibility.Public,
            isOverride,
            "void",
            ImmutableArray.Create<(ITypeSymbol, string)>((genericReaderInterface, "reader"))
        );

        var bodyIndent = $"{indent}    ";
        var innerIndent = $"{bodyIndent}    ";

        if (isOverride)
        {
            source.AppendLine($"{bodyIndent}base.Deserialize(reader);");
            source.AppendLine();
        }

        var afterDeserialization = classSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Select(
                m =>
                {
                    if (!m.ReturnsVoid || m.Parameters.Length != 0)
                    {
                        return (m, null);
                    }

                    return (m, m.GetAttributes()
                        .FirstOrDefault(
                            attr => SymbolEqualityComparer.Default.Equals(
                                attr.AttributeClass,
                                compilation.GetTypeByMetadataName(SymbolMetadata.AFTER_DESERIALIZATION_ATTRIBUTE)
                            )
                        ));
                }
            ).Where(m => m.Item2 != null).ToList();

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

        if (serializableFieldSaveFlagMethodsDictionary.Count > 0)
        {
            source.AppendLine();
            source.AppendLine($"{bodyIndent}var saveFlags = reader.ReadEnum<SaveFlag>();");
        }

        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var rule = SerializableMigrationRulesEngine.Rules[field.Rule];

            if (serializableFieldSaveFlagMethodsDictionary.TryGetValue(
                    field.Order,
                    out var serializableFieldSaveFlagMethods
                ))
            {
                source.AppendLine();
                // Special case
                if (field.Type == "bool")
                {
                    source.AppendLine($"{bodyIndent}{field.FieldName} = (saveFlags & SaveFlag.{field.Name}) != 0;");
                }
                else
                {
                    source.AppendLine($"{bodyIndent}if ((saveFlags & SaveFlag.{field.Name}) != 0)\n{bodyIndent}{{");
                    rule.GenerateDeserializationMethod(
                        source,
                        innerIndent,
                        compilation,
                        field,
                        parentReference
                    );
                    (rule as IPostDeserializeMethod)?.PostDeserializeMethod(
                        source,
                        innerIndent,
                        field,
                        compilation,
                        classSymbol
                    );

                    if (serializableFieldSaveFlagMethods.GetFieldDefaultValue != null)
                    {
                        source.AppendLine($"{bodyIndent}}}\n{bodyIndent}else\n{bodyIndent}{{");
                        source.AppendLine(
                            $"{bodyIndent}    {field.FieldName} = {serializableFieldSaveFlagMethods.GetFieldDefaultValue.Name}();"
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
                    compilation,
                    field,
                    parentReference
                );
                (rule as IPostDeserializeMethod)?.PostDeserializeMethod(
                    source,
                    bodyIndent,
                    field,
                    compilation,
                    classSymbol
                );
            }
        }

        source.GenerateAfterDeserialization($"{bodyIndent}", afterDeserialization);
        source.GenerateMethodEnd(indent);
    }

    private static void GenerateAfterDeserialization(
        this StringBuilder source, string indent, IList<(IMethodSymbol, AttributeData?)> afterDeserialization
    )
    {
        foreach (var (method, attr) in afterDeserialization)
        {
            if ((bool)attr.ConstructorArguments[0].Value!)
            {
                source.AppendLine($"{indent}{method.Name}();");
            }
            else
            {
                source.AppendLine($"{indent}Timer.DelayCall({method.Name});");
            }
        }
    }
}
