/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.Class.cs                           *
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
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    public static (string?, Diagnostic[]) GenerateSerializationPartialClass(
        this Compilation compilation,
        SerializableClassRecord classRecord,
        JsonSerializerOptions? jsonSerializerOptions,
        string? migrationPath,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var (
            classNode,
            classSymbol,
            serializableAttr,
            fields,
            properties,
            saveFlagMethods,
            defaultMethods,
            dirtyTrackingEntity,
            migrations
        ) = classRecord;

        var serializableFieldAttrAttribute =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTR_ATTRIBUTE);

        // If we have a parent that is or derives from ISerializable, then we are in override
        var isOverride = classSymbol.BaseType.HasSerializableInterface(compilation);
        var isSerializable = classSymbol.HasSerializableInterface(compilation);

        var version = (int)serializableAttr.ConstructorArguments[0].Value!;
        var encodedVersion = (bool)serializableAttr.ConstructorArguments[1].Value!;

        // Let's find out if we need to do serialization flags
        var serializableFieldSaveFlags = new SortedDictionary<int, SerializableFieldSaveFlagMethods>();
        for (var i = 0; i < saveFlagMethods.Length; i++)
        {
            var (symbol, attrData) = saveFlagMethods[i];
            var order = (int)attrData.ConstructorArguments[0].Value!;

            if (order < 0)
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE, symbol.Name);
                return (null, new[] { diag });
            }

            // Duplicate found, failure.
            if (serializableFieldSaveFlags.TryGetValue(order, out _))
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE, order);
                return (null, new[] { diag });
            }

            serializableFieldSaveFlags[order] = new SerializableFieldSaveFlagMethods
            {
                DetermineFieldShouldSerialize = (IMethodSymbol)symbol
            };
        }

        for (var i = 0; i < defaultMethods.Length; i++)
        {
            var (symbol, attrData) = defaultMethods[i];
            var order = (int)attrData.ConstructorArguments[0].Value!;

            if (order < 0)
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE, symbol.Name);
                return (null, new[] { diag });
            }

            // No default, so we remove the save flag too.
            if (!serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods))
            {
                serializableFieldSaveFlags.Remove(order);
                continue;
            }

            // Duplicate found, failure.
            if (serializableFieldSaveFlagMethods.GetFieldDefaultValue != null)
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE, order);
                return (null, new[] { diag });
            }

            serializableFieldSaveFlags[order] = serializableFieldSaveFlagMethods with
            {
                GetFieldDefaultValue = (IMethodSymbol)symbol
            };
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        StringBuilder source = new StringBuilder();

        source.AppendLine("#pragma warning disable\n");
        source.GenerateNamespaceStart(namespaceName);

        var indent = "    ";

        source.RecursiveGenerateClassStart(classSymbol, ImmutableArray<ITypeSymbol>.Empty, ref indent);

        source.GenerateField(
            indent,
            Accessibility.Private,
            InstanceModifier.Const,
            "int",
            "_version",
            version.ToString()
        );
        source.AppendLine();

        var hasMarkDirtyMethod = isSerializable || classSymbol.HasMarkDirtyMethod();

        var serializableFieldSet = new SortedSet<SerializableProperty>(new SerializablePropertyComparer());

        foreach (var (symbol, attributeData) in properties)
        {
            var attrCtorArgs = attributeData.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;

            if (order < 0)
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_PROPERTY_ATTRIBUTE, symbol.Name);
                return (null, new[] { diag });
            }

            var useField = (string)attrCtorArgs[1].Value!;

            if (symbol is IPropertySymbol propertySymbol)
            {
                var createField = string.IsNullOrWhiteSpace(useField);
                var fieldName = createField ? propertySymbol.Name.GetFieldName() : useField;
                var fieldType = propertySymbol.Type;

                // useField was not specified, so we are creating the field
                if (createField)
                {
                    source.GenerateField(
                        indent,
                        Accessibility.Private,
                        InstanceModifier.None,
                        fieldType.ToDisplayString(),
                        fieldName
                    );
                    source.AppendLine();
                }
                else
                {
                    // find the member
                    if (classSymbol.GetMembers(fieldName)
                            .FirstOrDefault(member => member is IFieldSymbol) is not IFieldSymbol fieldMember)
                    {
                        var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3004, fieldName, order);
                        return (null, new[] { diag });
                    }

                    fieldType = fieldMember.Type;
                }

                serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods);

                var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                    compilation,
                    propertySymbol.Name,
                    fieldType,
                    order,
                    propertySymbol.GetAttributes(),
                    classSymbol,
                    serializableFieldSaveFlagMethods
                ) with {
                    FieldName = fieldName
                };

                // We can't continue if we have duplicates.
                if (!serializableFieldSet.Add(serializableProperty))
                {
                    var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_PROPERTY_ATTRIBUTE, order);
                    return (null, new[] { diag });
                }
            }
        }

        foreach (var (symbol, attributeData) in fields)
        {
            token.ThrowIfCancellationRequested();

            var allAttributes = symbol.GetAttributes();

            foreach (var attr in allAttributes)
            {
                token.ThrowIfCancellationRequested();

                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, serializableFieldAttrAttribute))
                {
                    continue;
                }

                if (attr.AttributeClass == null)
                {
                    continue;
                }

                var ctorArgs = attr.ConstructorArguments;
                var attrTypeArg = ctorArgs[0];

                if (attrTypeArg.Kind == TypedConstantKind.Primitive && attrTypeArg.Value is string attrStr)
                {
                    source.AppendLine($"{indent}{attrStr}");
                }
                else
                {
                    var attrType = (ITypeSymbol)attrTypeArg.Value;
                    source.GenerateAttribute(indent, attrType?.Name, ctorArgs[1].Values);
                }
            }

            var attrCtorArgs = attributeData.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;

            if (order < 0)
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE, symbol.Name);
                return (null, new[] { diag });
            }

            var getterAccessor = Helpers.GetAccessibility(attrCtorArgs[1].Value?.ToString());
            var setterAccessor = Helpers.GetAccessibility(attrCtorArgs[2].Value?.ToString());
            var virtualProperty = (bool)attrCtorArgs[3].Value!;

            if (symbol is IFieldSymbol fieldSymbol)
            {
                source.GenerateSerializableProperty(
                    compilation,
                    indent,
                    fieldSymbol,
                    getterAccessor,
                    setterAccessor,
                    virtualProperty,
                    hasMarkDirtyMethod || dirtyTrackingEntity != null ? "this" : null
                );
                source.AppendLine();

                serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods);

                var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                    compilation,
                    fieldSymbol.Name.GetPropertyName(),
                    fieldSymbol.Type,
                    order,
                    allAttributes,
                    classSymbol,
                    serializableFieldSaveFlagMethods
                ) with {
                    FieldName = fieldSymbol.Name
                };

                // We can't continue if we have duplicates.
                if (!serializableFieldSet.Add(serializableProperty))
                {
                    var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE, order);
                    return (null, new[] { diag });
                }
            }
        }

        var serializableFields = serializableFieldSet.ToImmutableArray();
        for (var i = 0; i < serializableFields.Length; i++)
        {
            token.ThrowIfCancellationRequested();

            // They are out of order! (missing a number)
            var order = serializableFields[i].Order;
            if (order != i)
            {
                var diag = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3005, i, order);
                return (null, new[] { diag });
            }
        }

        if (isSerializable)
        {
            // Serial constructor
            source.GenerateSerialCtor(compilation, className, indent, isOverride);
            source.AppendLine();
        }

        var migrationsBuilder = ImmutableArray.CreateBuilder<SerializableMetadata>();

        for (var i = 0; i < version; i++)
        {
            token.ThrowIfCancellationRequested();

            if (!migrations.TryGetValue(i, out var additionalText))
            {
                continue;
            }

            var migrationSource = additionalText.GetText(token);
            if (migrationSource == null)
            {
                continue;
            }

            var chrArray = ArrayPool<char>.Shared.Rent(migrationSource.Length);
            migrationSource.CopyTo(0, chrArray, 0, migrationSource.Length);
            ReadOnlySpan<char> buffer = chrArray.AsSpan(0, migrationSource.Length);
            SerializableMetadata migration = JsonSerializer.Deserialize<SerializableMetadata>(buffer, jsonSerializerOptions);
            ArrayPool<char>.Shared.Return(chrArray);

            source.GenerateMigrationContentStruct(compilation, indent, migration, classSymbol);
            source.AppendLine();

            migrationsBuilder.Add(migration);
        }

        if (!isOverride && isSerializable)
        {
            // long ISerializable.SavePosition { get; set; } = -1;
            source.GenerateAutoProperty(
                Accessibility.NotApplicable,
                "long",
                "ISerializable.SavePosition",
                Accessibility.NotApplicable,
                Accessibility.NotApplicable,
                indent,
                defaultValue: "-1"
            );

            // BufferWriter ISerializable.SaveBuffer { get; set; }
            source.GenerateAutoProperty(
                Accessibility.NotApplicable,
                "BufferWriter",
                "ISerializable.SaveBuffer",
                Accessibility.NotApplicable,
                Accessibility.NotApplicable,
                indent
            );
        }

        if (!isSerializable && dirtyTrackingEntity != null)
        {
            source.GenerateMethodStart(
                indent,
                "MarkDirty",
                Accessibility.Public,
                false,
                "void",
                ImmutableArray<(ITypeSymbol, string)>.Empty
            );

            source.AppendLine($"{indent}    {dirtyTrackingEntity.Name}.MarkDirty();");

            source.GenerateMethodEnd(indent);
            source.AppendLine();
        }

        // Serialize Method
        source.GenerateSerializeMethod(
            compilation,
            indent,
            isOverride,
            encodedVersion,
            serializableFields,
            serializableFieldSaveFlags
        );
        source.AppendLine();

        // Deserialize Method
        source.GenerateDeserializeMethod(
            compilation,
            classSymbol,
            indent,
            isOverride,
            version,
            encodedVersion,
            migrationsBuilder.ToImmutable(),
            serializableFields,
            hasMarkDirtyMethod || dirtyTrackingEntity != null ? "this" : null,
            serializableFieldSaveFlags
        );

        // Serialize SaveFlag enum class
        if (serializableFieldSaveFlags.Count > 0)
        {
            source.AppendLine();
            source.GenerateEnumStart(
                "SaveFlag",
                $"{indent}    ",
                true,
                Accessibility.Private
            );

            source.GenerateEnumValue($"{indent}        ", true, "None", -1);
            int index = 0;
            foreach (var (order, _) in serializableFieldSaveFlags)
            {
                source.GenerateEnumValue($"{indent}        ", true, serializableFields[order].Name, index++);
            }

            source.GenerateEnumEnd($"{indent}    ");
        }

        source.RecursiveGenerateClassEnd(classSymbol, ref indent);
        source.GenerateNamespaceEnd();

        if (migrationPath != null)
        {
            // Write the migration file
            var newMigration = new SerializableMetadata
            {
                Version = version,
                Type = classSymbol.ToDisplayString(),
                Properties = serializableFields.Length > 0 ? serializableFields : null
            };

            WriteMigration(migrationPath, newMigration, jsonSerializerOptions, token);
        }

        return (source.ToString(), Array.Empty<Diagnostic>());
    }

    private static Regex _newLineRegex;

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
            _newLineRegex ??= new Regex(@"\r\n|\n\r|\n|\r");
            fileContents = _newLineRegex.Replace(fileContents, "\n");
        }
        File.WriteAllText(filePath, fileContents);
    }

    private static void RecursiveGenerateClassStart(
        this StringBuilder source,
        INamedTypeSymbol classSymbol,
        ImmutableArray<ITypeSymbol> interfaces,
        ref string indent
    )
    {
        var containingSymbolList = new List<INamedTypeSymbol>();

        do
        {
            containingSymbolList.Add(classSymbol);
            classSymbol = classSymbol.ContainingSymbol as INamedTypeSymbol;
        } while (classSymbol != null);

        containingSymbolList.Reverse();

        for (var i = 0; i < containingSymbolList.Count; i++)
        {
            var symbol = containingSymbolList[i];
            source.GenerateClassStart(symbol, indent, i == containingSymbolList.Count - 1 ? interfaces : ImmutableArray<ITypeSymbol>.Empty);
            indent += "    ";
        }
    }

    private static void RecursiveGenerateClassEnd(this StringBuilder source, INamedTypeSymbol classSymbol, ref string indent)
    {
        do
        {
            indent = indent.Substring(0, indent.Length - 4);
            source.GenerateClassEnd(indent);

            classSymbol = classSymbol.ContainingSymbol as INamedTypeSymbol;
        } while (classSymbol != null);
    }
}
