/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.BuildModel.cs                      *
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
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    /// <summary>
    /// Resolves every symbol- and compilation-dependent fact code generation needs into a
    /// value-equatable <see cref="SerializationModel" />. Runs in the incremental transform;
    /// an edit that does not change the serialization surface produces an equal model and
    /// nothing downstream re-runs.
    /// </summary>
    public static SerializationModelResult BuildSerializationModel(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var typeNode = (TypeDeclarationSyntax)ctx.TargetNode;
        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var compilation = ctx.SemanticModel.Compilation;
        var serializableAttr = ctx.Attributes[0];
        var location = LocationInfo.Create(typeNode.GetLocation());

        SerializationModelResult Fail(DiagnosticDescriptor descriptor, params object[] args) =>
            new(null, new[] { DiagnosticInfo.Create(descriptor, typeNode.GetLocation(), args) }.ToEquatableArray());

        if (!typeNode.IsPartial())
        {
            return Fail(DiagnosticDescriptors.SG3001, classSymbol.Name);
        }

        // The generator emits Deserialize for value types; a user-declared one collides with it.
        if (classSymbol.IsValueType && classSymbol.HasDeserializationCapability(compilation, out _))
        {
            return Fail(DiagnosticDescriptors.SG3009, classSymbol.Name);
        }

        // Gather annotated members from the attributed declaration.
        var fields = new List<(ISymbol, AttributeData)>();
        var properties = new List<(ISymbol, AttributeData)>();
        ISymbol? dirtyTrackingEntity = null;

        foreach (var m in typeNode.Members)
        {
            token.ThrowIfCancellationRequested();

            if (m is PropertyDeclarationSyntax propertyNode)
            {
                if (ctx.SemanticModel.GetDeclaredSymbol(propertyNode) is IPropertySymbol propertySymbol)
                {
                    if (propertySymbol.TryGetDirtyTrackingEntityField(compilation))
                    {
                        dirtyTrackingEntity = propertySymbol;
                    }
                    else if (propertySymbol.TryGetSerializableProperty(compilation, out var attributeData))
                    {
                        properties.Add((propertySymbol, attributeData));
                    }
                }
            }
            else if (m is FieldDeclarationSyntax fieldNode)
            {
                foreach (var variable in fieldNode.Declaration.Variables)
                {
                    token.ThrowIfCancellationRequested();

                    if (ctx.SemanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol)
                    {
                        if (fieldSymbol.TryGetDirtyTrackingEntityField(compilation))
                        {
                            dirtyTrackingEntity = fieldSymbol;
                        }
                        else if (fieldSymbol.TryGetSerializableField(compilation, out var attributeData))
                        {
                            fields.Add((fieldSymbol, attributeData));
                        }
                    }
                }
            }
        }

        var isValueType = classSymbol.IsValueType;
        var isOverride = classSymbol.BaseType.HasSerializableInterface(compilation);
        var isSerializable = classSymbol.HasSerializableInterface(compilation);

        var version = (int)serializableAttr.ConstructorArguments[0].Value!;
        var encodedVersion = (bool)serializableAttr.ConstructorArguments[1].Value!;

        static bool IsSaveFlagShape(IMethodSymbol method) =>
            method is { ReturnsVoid: false, Parameters.Length: 0, ReturnType.SpecialType: SpecialType.System_Boolean };

        static bool IsDefaultValueShape(IMethodSymbol method, ITypeSymbol fieldType) =>
            method is { ReturnsVoid: false, Parameters.Length: 0 } &&
            SymbolEqualityComparer.Default.Equals(method.ReturnType, fieldType);

        static bool IsChangedShape(IMethodSymbol method, ITypeSymbol fieldType) =>
            method is { ReturnsVoid: true, Parameters.Length: 2 } &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, fieldType) &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, fieldType);

        static bool IsAllowChangeShape(IMethodSymbol method, ITypeSymbol fieldType) =>
            method is { ReturnsVoid: false, Parameters.Length: 1, ReturnType.SpecialType: SpecialType.System_Boolean } &&
            method.Parameters[0].RefKind == RefKind.Ref &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, fieldType);

        // Dirty tracking / MarkDirty resolution.
        var parentTypeHasEntityTracking = false;
        if (!isSerializable && dirtyTrackingEntity == null)
        {
            dirtyTrackingEntity = classSymbol.BaseType?.HasDirtyTrackingEntity(compilation);
            if (dirtyTrackingEntity != null)
            {
                parentTypeHasEntityTracking = true;
            }
        }

        var dirtyTrackingEntityNull =
            dirtyTrackingEntity?.GetAttributes().Any(a => a.IsCanBeNull(compilation)) ?? false;

        var emitMarkDirtyMethod = false;
        string? markDirtyBody = null;
        string? markDirtyMethod;

        if (dirtyTrackingEntity != null)
        {
            if (!parentTypeHasEntityTracking)
            {
                emitMarkDirtyMethod = true;

                var dirtyTrackingType = (dirtyTrackingEntity as IFieldSymbol)?.Type ??
                                        (dirtyTrackingEntity as IPropertySymbol)?.Type;

                markDirtyBody = dirtyTrackingType?.GetMarkDirtyMethod(
                    dirtyTrackingEntity: dirtyTrackingEntity.Name,
                    isSerializable: dirtyTrackingType?.HasSerializableInterface(compilation) ?? false,
                    dirtyCanBeNull: dirtyTrackingEntityNull
                );
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

        // Linkage: [SaveFlag] and [DeserializeTimer] on the serializable members themselves.
        // The named methods must exist with the expected shapes.
        var serializableFieldSaveFlags = new SortedDictionary<int, SerializableFieldSaveFlagMethods>();
        var timerLinks = new Dictionary<int, TimerFieldModel>();

        foreach (var (symbol, attributeData) in fields.Concat(properties))
        {
            token.ThrowIfCancellationRequested();

            var order = (int)attributeData.ConstructorArguments[0].Value!;
            if (order < 0)
            {
                continue;
            }

            var memberType = (symbol as IFieldSymbol)?.Type ?? ((IPropertySymbol)symbol).Type;

            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.IsSaveFlag(compilation))
                {
                    var shouldName = attr.ConstructorArguments[0].Value as string;
                    var shouldMethod = classSymbol.FindLinkedMethod(shouldName, IsSaveFlagShape);
                    if (shouldMethod == null)
                    {
                        return Fail(DiagnosticDescriptors.SG3015, shouldName ?? "", "SaveFlag", "bool Method()");
                    }

                    IMethodSymbol? defaultMethod = null;
                    var defaultName = attr.ConstructorArguments[1].Value as string;
                    if (defaultName != null)
                    {
                        defaultMethod = classSymbol.FindLinkedMethod(defaultName, m => IsDefaultValueShape(m, memberType));
                        if (defaultMethod == null)
                        {
                            return Fail(DiagnosticDescriptors.SG3015, defaultName, "SaveFlag", $"{memberType} Method()");
                        }
                    }

                    serializableFieldSaveFlags[order] = new SerializableFieldSaveFlagMethods
                    {
                        DetermineFieldShouldSerialize = shouldMethod,
                        GetFieldDefaultValue = defaultMethod
                    };
                }
                else if (attr.IsDeserializeTimer(compilation))
                {
                    var methodName = attr.ConstructorArguments[0].Value as string;
                    var method = classSymbol.FindLinkedMethod(
                        methodName,
                        m => m is { ReturnsVoid: true, Parameters.Length: 1 } && m.Parameters[0].Type.IsTimeSpan(compilation)
                    );

                    if (method == null)
                    {
                        return Fail(DiagnosticDescriptors.SG3015, methodName ?? "", "DeserializeTimer", "void Method(TimeSpan delay)");
                    }

                    timerLinks[order] = new TimerFieldModel(order, method.Name);
                }
            }
        }

        var serializableFieldSet = new SortedSet<SerializableProperty>(new SerializablePropertyComparer());
        var backingFields = new List<BackingFieldModel>();

        foreach (var (symbol, attributeData) in properties)
        {
            var attrCtorArgs = attributeData.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;

            if (order < 0)
            {
                return Fail(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_PROPERTY_ATTRIBUTE, symbol.Name);
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
                    backingFields.Add(new BackingFieldModel(fieldType.ToDisplayString(), fieldName));
                }
                else
                {
                    if (classSymbol.GetMembers(fieldName)
                            .FirstOrDefault(member => member is IFieldSymbol) is not IFieldSymbol fieldMember)
                    {
                        return Fail(DiagnosticDescriptors.SG3004, fieldName, order);
                    }

                    fieldType = fieldMember.Type;
                }

                serializableFieldSaveFlags.TryGetValue(order, out var saveFlagMethodsForOrder);

                try
                {
                    var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                        compilation,
                        propertySymbol.Name,
                        fieldType,
                        order,
                        propertySymbol.GetAttributes(),
                        classSymbol,
                        saveFlagMethodsForOrder
                    ) with {
                        FieldName = fieldName
                    };

                    if (!serializableFieldSet.Add(serializableProperty))
                    {
                        return Fail(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_PROPERTY_ATTRIBUTE, order);
                    }
                }
                catch (NoRuleFoundException e)
                {
                    return Fail(DiagnosticDescriptors.SG3007, e.PropertyName, e.PropertyType);
                }
            }
        }

        var fieldEmissions = new List<FieldPropertyModel>();

        foreach (var (symbol, attributeData) in fields)
        {
            token.ThrowIfCancellationRequested();

            var allAttributes = symbol.GetAttributes();

            var attributeLines = new List<string>();
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
                    attributeLines.Add($"[{attrType}]");
                }
                else
                {
                    var attrSource = new StringBuilder();
                    attrSource.GenerateAttribute("", attrType, attr.ConstructorArguments);
                    attributeLines.Add(attrSource.ToString().TrimEnd('\r', '\n'));
                }
            }

            var attrCtorArgs = attributeData.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;

            if (order < 0)
            {
                return Fail(DiagnosticDescriptors.SG3006, SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE, symbol.Name);
            }

            var getterAccessor = Helpers.GetAccessibility(attrCtorArgs[1].Value?.ToString());
            var setterAccessor = Helpers.GetAccessibility(attrCtorArgs[2].Value?.ToString());
            var virtualProperty = (bool)attrCtorArgs[3].Value!;

            if (symbol is IFieldSymbol fieldSymbol)
            {
                // Readonly fields cannot have setters - force to null
                var effectiveSetterAccessor = fieldSymbol.IsReadOnly ? (Accessibility?)null : setterAccessor;

                // The setter hooks are part of [SerializableField] itself, so they cannot be
                // declared on a member without a generated setter to invoke them.
                IMethodSymbol? fieldChangedMethod = null;
                var fieldChangedName = attrCtorArgs.Length > 4 ? attrCtorArgs[4].Value as string : null;
                if (fieldChangedName != null)
                {
                    if (effectiveSetterAccessor == null)
                    {
                        return Fail(DiagnosticDescriptors.SG3018, "fieldChanged", fieldSymbol.Name);
                    }

                    fieldChangedMethod = classSymbol.FindLinkedMethod(
                        fieldChangedName,
                        m => IsChangedShape(m, fieldSymbol.Type)
                    );

                    if (fieldChangedMethod == null)
                    {
                        return Fail(
                            DiagnosticDescriptors.SG3015, fieldChangedName, "SerializableField fieldChanged",
                            $"void Method({fieldSymbol.Type} oldValue, {fieldSymbol.Type} newValue)"
                        );
                    }
                }

                IMethodSymbol? allowFieldChangeMethod = null;
                var allowFieldChangeName = attrCtorArgs.Length > 5 ? attrCtorArgs[5].Value as string : null;
                if (allowFieldChangeName != null)
                {
                    if (effectiveSetterAccessor == null)
                    {
                        return Fail(DiagnosticDescriptors.SG3018, "allowFieldChange", fieldSymbol.Name);
                    }

                    allowFieldChangeMethod = classSymbol.FindLinkedMethod(
                        allowFieldChangeName,
                        m => IsAllowChangeShape(m, fieldSymbol.Type)
                    );

                    if (allowFieldChangeMethod == null)
                    {
                        return Fail(
                            DiagnosticDescriptors.SG3015, allowFieldChangeName, "SerializableField allowFieldChange",
                            $"bool Method(ref {fieldSymbol.Type} value)"
                        );
                    }
                }

                var invalidateProperties = allAttributes.Any(
                    attr => attr.AttributeClass?.Equals(
                        compilation.GetCachedTypeByMetadataName(SymbolMetadata.INVALIDATE_PROPERTIES_ATTRIBUTE),
                        SymbolEqualityComparer.Default
                    ) ?? false
                );

                // Data structure method facts.
                var propertyType = fieldSymbol.Type;
                var namedTypeSymbol = propertyType as INamedTypeSymbol;
                var elementType = (propertyType as IArrayTypeSymbol)?.ElementType ??
                                  (namedTypeSymbol?.TypeArguments.Length > 0 ? namedTypeSymbol.TypeArguments[0] : null);

                var dsIsArray = false;
                var dsIsDictionary = false;
                var dsIsList = false;
                var dsIsCollection = false;
                string? dsElementType = null;
                string? dsValueType = null;

                if (!fieldSymbol.IsReadOnly && elementType != null)
                {
                    dsIsArray = propertyType is IArrayTypeSymbol;
                    dsIsDictionary = propertyType.IsDictionaryInterface(compilation);
                    dsIsList = propertyType.IsListInterface(compilation);
                    dsIsCollection = propertyType.IsCollection(compilation);
                    dsElementType = elementType.ToString();
                    dsValueType = dsIsDictionary ? namedTypeSymbol!.TypeArguments[1].ToString() : null;
                }

                fieldEmissions.Add(
                    new FieldPropertyModel(
                        order,
                        fieldSymbol.Name,
                        fieldSymbol.Type.ToString(),
                        fieldSymbol.Name.GetPropertyName(),
                        getterAccessor,
                        effectiveSetterAccessor,
                        fieldSymbol.IsReadOnly,
                        virtualProperty,
                        fieldSymbol.Type.HasInequalityOperator(),
                        invalidateProperties,
                        fieldChangedMethod?.Name,
                        allowFieldChangeMethod?.Name,
                        attributeLines.ToEquatableArray(),
                        dsIsArray,
                        dsIsDictionary,
                        dsIsList,
                        dsIsCollection,
                        dsElementType,
                        dsValueType
                    )
                );

                serializableFieldSaveFlags.TryGetValue(order, out var saveFlagMethodsForOrder);

                try
                {
                    var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                        compilation,
                        fieldSymbol.Name.GetPropertyName(),
                        fieldSymbol.Type,
                        order,
                        allAttributes,
                        classSymbol,
                        saveFlagMethodsForOrder
                    ) with {
                        FieldName = fieldSymbol.Name,
                        IsReadOnly = fieldSymbol.IsReadOnly
                    };

                    if (!serializableFieldSet.Add(serializableProperty))
                    {
                        return Fail(DiagnosticDescriptors.SG3003, SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE, order);
                    }
                }
                catch (NoRuleFoundException e)
                {
                    return Fail(DiagnosticDescriptors.SG3007, e.PropertyName, e.PropertyType);
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
                return Fail(DiagnosticDescriptors.SG3005, serializableFields[i].Name, i, order);
            }
        }

        // Timer fields need a [DeserializeTimer] declaration to rebuild the timer on load.
        var timerFields = new List<TimerFieldModel>();
        foreach (var field in serializableFields)
        {
            if (field.Rule != nameof(TimerMigrationRule))
            {
                continue;
            }

            if (!timerLinks.TryGetValue(field.Order, out var timerField))
            {
                return Fail(DiagnosticDescriptors.SG3008, field.Name);
            }

            timerFields.Add(timerField);
        }

        // AfterDeserialization callbacks, in member order.
        var afterDeserialization = new List<AfterDeserializeModel>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IMethodSymbol { ReturnsVoid: true, Parameters.Length: 0 } method)
            {
                continue;
            }

            var attr = method.GetAttributes()
                .FirstOrDefault(
                    a => SymbolEqualityComparer.Default.Equals(
                        a.AttributeClass,
                        compilation.GetCachedTypeByMetadataName(SymbolMetadata.AFTER_DESERIALIZATION_ATTRIBUTE)
                    )
                );

            if (attr != null)
            {
                afterDeserialization.Add(new AfterDeserializeModel(method.Name, (bool)attr.ConstructorArguments[0].Value!));
            }
        }

        // Save flag method names, in order.
        var saveFlagModels = serializableFieldSaveFlags
            .Select(
                kvp => new SaveFlagModel(
                    kvp.Key,
                    kvp.Value.DetermineFieldShouldSerialize!.Name,
                    kvp.Value.GetFieldDefaultValue?.Name
                )
            )
            .ToList();

        // The nesting chain, outermost first.
        var shells = new List<TypeShellModel>();
        var containing = classSymbol;
        while (containing != null)
        {
            shells.Add(
                new TypeShellModel(
                    containing.DeclaredAccessibility.ToFriendlyString(),
                    containing.GetClassNameWithTypeParameters(),
                    containing.GetWhereClausesForTypeParameters()
                )
            );
            containing = containing.ContainingSymbol as INamedTypeSymbol;
        }

        shells.Reverse();

        var model = new SerializationModel(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            classSymbol.ToDisplayString(),
            classSymbol.GetGenericArityName(),
            typeNode.GetTypeKeyword(),
            isValueType,
            version,
            encodedVersion,
            isOverride,
            isOverride || classSymbol.BaseType.IsSerializableRecursive(compilation),
            isSerializable,
            classSymbol.HasSerialCtor(compilation),
            emitMarkDirtyMethod,
            markDirtyBody,
            markDirtyMethod,
            dirtyTrackingEntity?.Name ?? "this",
            shells.ToEquatableArray(),
            backingFields.ToEquatableArray(),
            serializableFields.ToEquatableArray(),
            fieldEmissions.ToEquatableArray(),
            saveFlagModels.ToEquatableArray(),
            afterDeserialization.ToEquatableArray(),
            timerFields.ToEquatableArray(),
            location
        );

        return new SerializationModelResult(model, System.Array.Empty<DiagnosticInfo>().ToEquatableArray());
    }
}
