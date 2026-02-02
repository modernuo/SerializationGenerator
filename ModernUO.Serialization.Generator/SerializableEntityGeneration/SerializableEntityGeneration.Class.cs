/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public static (string?, SerializableMetadata, Diagnostic[]) GenerateSerializationPartialClass(
        this Compilation compilation,
        SerializableClassRecord classRecord,
        JsonSerializerOptions? jsonSerializerOptions,
        bool generateMetadata,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var (
            typeNode,
            classSymbol,
            serializableAttr,
            fields,
            properties,
            saveFlagMethods,
            defaultMethods,
            changedMethods,
            dirtyTrackingEntity,
            migrations
        ) = classRecord;

        var typeKeyword = typeNode.GetTypeKeyword();
        var isValueType = classRecord.IsValueType;

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
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE, symbol.Name);
                return (null, null, [diag]);
            }

            // Duplicate found, failure.
            if (serializableFieldSaveFlags.TryGetValue(order, out _))
            {
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE, order);
                return (null, null, [diag]);
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
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE, symbol.Name);
                return (null, null, [diag]);
            }

            // No save flag, so we ignore the default value
            if (!serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods))
            {
                continue;
            }

            // Duplicate found, failure.
            if (serializableFieldSaveFlagMethods.GetFieldDefaultValue != null)
            {
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE, order);
                return (null, null, [diag]);
            }

            serializableFieldSaveFlags[order] = serializableFieldSaveFlagMethods with
            {
                GetFieldDefaultValue = (IMethodSymbol)symbol
            };
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        StringBuilder source = new StringBuilder();

        source.AppendLine($"""
                           // <auto-generated>
                           //     This code was generated by the ModernUO Serialization Generator tool.
                           //     Version: {Version}
                           //
                           //     Changes to this file may cause incorrect behavior and will be lost if
                           //     the code is regenerated.
                           // </auto-generated>

                           """);

        source.AppendLine("#pragma warning disable\n");
        source.GenerateNamespaceStart(namespaceName);

        var indent = "    ";

        source.RecursiveGenerateClassStart(classSymbol, ImmutableArray<ITypeSymbol>.Empty, ref indent, typeKeyword);

        source.GenerateField(
            indent,
            Accessibility.Private,
            InstanceModifier.Const,
            "int",
            "SerializationVersion",
            version.ToString()
        );
        source.AppendLine();

        var parentTypeHasEntityTracking = false;
        if (!isSerializable && dirtyTrackingEntity == null)
        {
            dirtyTrackingEntity = classSymbol.BaseType?.HasDirtyTrackingEntity(compilation);
            if (dirtyTrackingEntity != null)
            {
                parentTypeHasEntityTracking = true;
            }
        }

        var dirtyTrackingEntityNull = dirtyTrackingEntity?.GetAttributes().Any(a => a.IsCanBeNull(compilation)) ?? false;
        string markDirtyMethod;


        if (dirtyTrackingEntity != null)
        {
            if (!parentTypeHasEntityTracking)
            {
                source.GenerateMethodStart(
                    indent,
                    "MarkDirty",
                    Accessibility.Public,
                    false,
                    "void",
                    ImmutableArray<(ITypeSymbol, string)>.Empty
                );

                var dirtyTrackingType = (dirtyTrackingEntity as IFieldSymbol)?.Type ??
                                        (dirtyTrackingEntity as IPropertySymbol)?.Type;

                var dirtyTrackingDirtyMethod =
                    dirtyTrackingType?.GetMarkDirtyMethod(
                        dirtyTrackingEntity: dirtyTrackingEntity.Name,
                        isSerializable: dirtyTrackingType?.HasSerializableInterface(compilation) ?? false,
                        dirtyCanBeNull: dirtyTrackingEntityNull
                    );

                source.AppendLine($"{indent}    {dirtyTrackingDirtyMethod};");

                source.GenerateMethodEnd(indent);
                source.AppendLine();
            }

            markDirtyMethod = "MarkDirty()";
        }
        else if (isSerializable)
        {
            markDirtyMethod = classSymbol.GetMarkDirtyMethod();
        }
        else
        {
            markDirtyMethod = null;
        }

        // Process SerializableFieldChanged methods into dictionary by order
        var serializableFieldChangedMethods = new Dictionary<int, IMethodSymbol>();
        for (var i = 0; i < changedMethods.Length; i++)
        {
            var (symbol, attrData) = changedMethods[i];
            var order = (int)attrData.ConstructorArguments[0].Value!;

            if (order < 0)
            {
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_CHANGED_ATTRIBUTE, symbol.Name);
                return (null, null, [diag]);
            }

            // Duplicate found, failure.
            if (serializableFieldChangedMethods.ContainsKey(order))
            {
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_CHANGED_ATTRIBUTE, order);
                return (null, null, [diag]);
            }

            serializableFieldChangedMethods[order] = (IMethodSymbol)symbol;
        }

        var serializableFieldSet = new SortedSet<SerializableProperty>(new SerializablePropertyComparer());

        foreach (var (symbol, attributeData) in properties)
        {
            var attrCtorArgs = attributeData.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;

            if (order < 0)
            {
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_PROPERTY_ATTRIBUTE, symbol.Name);
                return (null, null, [diag]);
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
                        var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3004, fieldName, order);
                        return (null, null, [diag]);
                    }

                    fieldType = fieldMember.Type;
                }

                serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods);

                try
                {
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
                        var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_PROPERTY_ATTRIBUTE, order);
                        return (null, null, [diag]);
                    }
                }
                catch (NoRuleFoundException e)
                {
                    var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3007, e.PropertyName, e.PropertyType);
                    return (null, null, [diag]);
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

                if (attr.AttributeClass == null)
                {
                    continue;
                }

                if (!attr.IsSerializedPropertyAttr(compilation, out var serializedPropertyAttrType))
                {
                    continue;
                }

                var attrType = serializedPropertyAttrType.ToDisplayString();

                if (attr.ConstructorArguments.Length == 0)
                {
                    source.AppendLine($"{indent}[{attrType}]");
                }
                else
                {
                    source.GenerateAttribute(indent, attrType, attr.ConstructorArguments);
                }
            }

            var attrCtorArgs = attributeData.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;

            if (order < 0)
            {
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE, symbol.Name);
                return (null, null, [diag]);
            }

            var getterAccessor = Helpers.GetAccessibility(attrCtorArgs[1].Value?.ToString());
            var setterAccessor = Helpers.GetAccessibility(attrCtorArgs[2].Value?.ToString());
            var virtualProperty = (bool)attrCtorArgs[3].Value!;

            if (symbol is IFieldSymbol fieldSymbol)
            {
                // Readonly fields cannot have setters - force to null
                var effectiveSetterAccessor = fieldSymbol.IsReadOnly ? (Accessibility?)null : setterAccessor;

                // Get the changed method for this field if any
                serializableFieldChangedMethods.TryGetValue(order, out var fieldChangedMethod);

                // Validate changed method signature if present
                if (fieldChangedMethod != null)
                {
                    var fieldType = fieldSymbol.Type;
                    var isValidSignature = fieldChangedMethod.ReturnsVoid &&
                                           fieldChangedMethod.Parameters.Length == 2 &&
                                           SymbolEqualityComparer.Default.Equals(fieldChangedMethod.Parameters[0].Type, fieldType) &&
                                           SymbolEqualityComparer.Default.Equals(fieldChangedMethod.Parameters[1].Type, fieldType);

                    if (!isValidSignature)
                    {
                        var diag = typeNode.GenerateDiagnostic(
                            DiagnosticDescriptors.SG3010,
                            fieldChangedMethod.Name,
                            fieldType.ToDisplayString()
                        );
                        return (null, null, [diag]);
                    }
                }

                source.GenerateSerializableProperty(
                    compilation,
                    indent,
                    fieldSymbol,
                    getterAccessor,
                    effectiveSetterAccessor,
                    virtualProperty,
                    markDirtyMethod,
                    fieldChangedMethod
                );
                source.AppendLine();

                // Skip data structure methods for readonly fields (they cannot be modified)
                var generatedDataStructureMethods = false;
                if (!fieldSymbol.IsReadOnly)
                {
                    var propertyAccessor = setterAccessor > getterAccessor ? setterAccessor : getterAccessor;
                    generatedDataStructureMethods = source.GenerateDataStructureMethods(
                        compilation,
                        indent,
                        fieldSymbol,
                        propertyAccessor.ToFriendlyString(),
                        markDirtyMethod
                    );
                }

                if (generatedDataStructureMethods)
                {
                    source.AppendLine();
                }

                serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods);

                try
                {
                    var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                        compilation,
                        fieldSymbol.Name.GetPropertyName(),
                        fieldSymbol.Type,
                        order,
                        allAttributes,
                        classSymbol,
                        serializableFieldSaveFlagMethods
                    ) with {
                        FieldName = fieldSymbol.Name,
                        IsReadOnly = fieldSymbol.IsReadOnly
                    };

                    // We can't continue if we have duplicates.
                    if (!serializableFieldSet.Add(serializableProperty))
                    {
                        var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE, order);
                        return (null, null, [diag]);
                    }
                }
                catch (NoRuleFoundException e)
                {
                    var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3007, e.PropertyName, e.PropertyType);
                    return (null, null, [diag]);
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
                var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3005, serializableFields[i].Name, i, order);
                return (null, null, [diag]);
            }
        }

        // Skip serial constructor for value types - they use static factory or instance Deserialize
        if (!isValueType && isSerializable && !classSymbol.HasSerialCtor(compilation))
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
            SerializableMetadata migration;
            try
            {
                migrationSource.CopyTo(0, chrArray, 0, migrationSource.Length);
                ReadOnlySpan<char> buffer = chrArray.AsSpan(0, migrationSource.Length);
                migration = JsonSerializer.Deserialize<SerializableMetadata>(buffer, jsonSerializerOptions);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chrArray);
            }

            source.GenerateMigrationContentStruct(compilation, indent, migration, classSymbol);
            source.AppendLine();

            migrationsBuilder.Add(migration);
        }

        var serializeOverride = isOverride || classSymbol.BaseType.IsSerializableRecursive(compilation);

        // Compute SaveFlag configuration
        // Maps field order to (enumIndex, bitIndex) for fields with save flags
        var saveFlagMapping = new Dictionary<int, (int EnumIndex, int BitIndex)>();
        var saveFlagCount = 0;
        foreach (var (order, _) in serializableFieldSaveFlags)
        {
            if (!serializableFields[order].IsReadOnly)
            {
                saveFlagCount++;
            }
        }

        var saveFlagUseUlong = saveFlagCount > 32;
        var saveFlagEnumCount = saveFlagCount <= 64 ? 1 : (saveFlagCount + 63) / 64;
        var bitsPerEnum = saveFlagUseUlong ? 64 : 32;

        var currentBitIndex = 0;
        var currentEnumIndex = 0;
        foreach (var (order, _) in serializableFieldSaveFlags)
        {
            if (serializableFields[order].IsReadOnly)
            {
                continue;
            }

            if (currentBitIndex >= bitsPerEnum)
            {
                currentBitIndex = 0;
                currentEnumIndex++;
            }

            saveFlagMapping[order] = (currentEnumIndex, currentBitIndex);
            currentBitIndex++;
        }

        // Serialize Method
        source.GenerateSerializeMethod(
            compilation,
            indent,
            serializeOverride,
            encodedVersion,
            serializableFields,
            serializableFieldSaveFlags,
            saveFlagMapping,
            saveFlagUseUlong,
            saveFlagEnumCount
        );
        source.AppendLine();

        // Deserialize Method
        try
        {
            source.GenerateDeserializeMethod(
                compilation,
                classSymbol,
                indent,
                serializeOverride,
                version,
                encodedVersion,
                migrationsBuilder.ToImmutable(),
                serializableFields,
                markDirtyMethod,
                dirtyTrackingEntity?.Name ?? "this",
                serializableFieldSaveFlags,
                saveFlagMapping,
                saveFlagUseUlong,
                saveFlagEnumCount
            );
        }
        catch (DeserializeTimerFieldRequiredException e)
        {
            var diag = typeNode.GenerateDiagnostic(DiagnosticDescriptors.SG3008, e.PropertyName);
            return (null, null, [diag]);
        }

        // Serialize SaveFlag enum class(es)
        if (saveFlagCount > 0)
        {
            // Reset for enum generation
            currentBitIndex = 0;
            currentEnumIndex = 0;
            var isFirstEnum = true;

            foreach (var (order, _) in serializableFieldSaveFlags)
            {
                // Skip readonly fields
                if (serializableFields[order].IsReadOnly)
                {
                    continue;
                }

                // Start a new enum if needed
                if (currentBitIndex == 0 || currentBitIndex >= bitsPerEnum)
                {
                    if (!isFirstEnum)
                    {
                        source.GenerateEnumEnd(indent);
                    }

                    // If we're starting a new enum due to overflow, increment the enum index first
                    if (currentBitIndex >= bitsPerEnum)
                    {
                        currentBitIndex = 0;
                        currentEnumIndex++;
                    }

                    source.AppendLine();
                    var enumName = currentEnumIndex == 0 ? "SaveFlag" : $"SaveFlag{currentEnumIndex + 1}";
                    source.GenerateEnumStart(
                        enumName,
                        indent,
                        true,
                        Accessibility.Private,
                        saveFlagUseUlong ? "ulong" : null
                    );

                    if (saveFlagUseUlong)
                    {
                        source.GenerateEnumValueLong($"{indent}    ", true, "None", -1);
                    }
                    else
                    {
                        source.GenerateEnumValue($"{indent}    ", true, "None", -1);
                    }

                    isFirstEnum = false;
                }

                if (saveFlagUseUlong)
                {
                    source.GenerateEnumValueLong($"{indent}    ", true, serializableFields[order].Name, currentBitIndex++);
                }
                else
                {
                    source.GenerateEnumValue($"{indent}    ", true, serializableFields[order].Name, currentBitIndex++);
                }
            }

            if (!isFirstEnum)
            {
                source.GenerateEnumEnd(indent);
            }
        }

        source.RecursiveGenerateClassEnd(classSymbol, ref indent);
        source.GenerateNamespaceEnd();

        // Write the migration file (exclude readonly fields since they aren't serialized)
        // Use arity notation for generic types to match file naming convention
        var serializableFieldsForMigration = serializableFields.Where(f => !f.IsReadOnly).ToImmutableArray();
        var newMigration = generateMetadata ? new SerializableMetadata
        {
            Version = version,
            Type = classSymbol.GetGenericArityName(),
            Properties = serializableFieldsForMigration.Length > 0 ? serializableFieldsForMigration : null
        } : null;

        return (source.ToString(), newMigration, null);
    }

    extension(StringBuilder source)
    {
        private void RecursiveGenerateClassStart(
            INamedTypeSymbol classSymbol,
            ImmutableArray<ITypeSymbol> interfaces,
            ref string indent,
            string typeKeyword = "class"
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
                var last = i == containingSymbolList.Count - 1;

                if (last)
                {
                    source.AppendLine($"{indent}[System.CodeDom.Compiler.GeneratedCode(\"ModernUO.Serialization.Generator\", \"{Version}\")]");
                }

                // Only the outermost type uses the specified type keyword
                var currentTypeKeyword = last ? typeKeyword : "class";
                source.GenerateClassStart(symbol, indent, last ? interfaces : ImmutableArray<ITypeSymbol>.Empty, true, currentTypeKeyword);
                indent += "    ";
            }
        }

        private void RecursiveGenerateClassEnd(INamedTypeSymbol classSymbol, ref string indent)
        {
            do
            {
                indent = indent.Substring(0, indent.Length - 4);
                source.GenerateClassEnd(indent);

                classSymbol = classSymbol.ContainingSymbol as INamedTypeSymbol;
            } while (classSymbol != null);
        }
    }
}
