using System.Text;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    // Null when the class has no dirty-tracking target (SG3019); emitting nothing beats a bare ';'.
    private static void AppendMarkDirty(this StringBuilder source, string indent, string? markDirtyMethod)
    {
        if (markDirtyMethod != null)
        {
            source.AppendLine($"{indent}    {markDirtyMethod};");
        }
    }

    private static void AppendLazyCreate(this StringBuilder source, string indent, FieldPropertyModel field)
    {
        if (field.DsCreateExpression != null)
        {
            source.AppendLine($"{indent}    {field.FieldName} ??= {field.DsCreateExpression};");
        }
    }

    // Returns whether the mutation reported a change, marking dirty only when it did.
    private static void AppendReturnIfChanged(this StringBuilder source, string indent, string changed, string? markDirtyMethod)
    {
        if (markDirtyMethod == null)
        {
            source.AppendLine($"{indent}    return {changed};");
            return;
        }

        source.AppendLine($"{indent}    if ({changed})");
        source.AppendLine($"{indent}    {{");
        source.AppendLine($"{indent}        {markDirtyMethod};");
        source.AppendLine($"{indent}        return true;");
        source.AppendLine($"{indent}    }}");
        source.AppendLine();
        source.AppendLine($"{indent}    return false;");
    }

    public static bool GenerateDataStructureMethods(
        this StringBuilder source,
        string indent,
        FieldPropertyModel field,
        string propertyAccessor,
        string? markDirtyMethod
    )
    {
        // Non-collection generics (e.g. KeyValuePair) have type arguments but no Add/Clear surface.
        if (!field.HasDataStructureMethods)
        {
            return false;
        }

        var propertyName = field.PropertyName;
        var fieldName = field.FieldName;
        var elementTypeName = field.DsElementType;

        // Mutations go through the backing field: lazily creating the collection through the
        // property setter would run fieldChanged/allowFieldChange for what is not a replacement.
        // Removals and clears tolerate a null collection, and only mark dirty on an actual change.
        // Return types mirror the BCL: a mutation that can be a no-op returns whether it changed anything.
        if (field.DsIsDictionary)
        {
            var valueTypeName = field.DsValueType;

            // Add
            source.AppendLine($"{indent}{propertyAccessor} bool AddTo{propertyName}({elementTypeName} key, {valueTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLazyCreate(indent, field);
            if (field.DsHasTryAdd)
            {
                source.AppendReturnIfChanged(indent, $"{fieldName}.TryAdd(key, value)", markDirtyMethod);
            }
            else
            {
                source.AppendLine($"{indent}    if ({fieldName}.ContainsKey(key))");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        return false;");
                source.AppendLine($"{indent}    }}");
                source.AppendLine();
                source.AppendLine($"{indent}    {fieldName}.Add(key, value);");
                source.AppendMarkDirty(indent, markDirtyMethod);
                source.AppendLine($"{indent}    return true;");
            }
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Remove
            source.AppendLine($"{indent}{propertyAccessor} bool RemoveFrom{propertyName}({elementTypeName} key)");
            source.AppendLine($"{indent}{{");
            source.AppendReturnIfChanged(indent, $"{fieldName}?.Remove(key) == true", markDirtyMethod);
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Replace
            source.AppendLine($"{indent}{propertyAccessor} void ReplaceIn{propertyName}({elementTypeName} key, {valueTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLazyCreate(indent, field);
            source.AppendLine($"{indent}    {fieldName}[key] = value;");
            source.AppendMarkDirty(indent, markDirtyMethod);
            source.AppendLine($"{indent}}}");
        }
        else if (field.DsIsCollection)
        {
            // Add
            var addReturnType = field.DsIsSet ? "bool" : "void";
            source.AppendLine($"{indent}{propertyAccessor} {addReturnType} AddTo{propertyName}({elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLazyCreate(indent, field);
            if (field.DsIsSet)
            {
                source.AppendReturnIfChanged(indent, $"{fieldName}.Add(value)", markDirtyMethod);
            }
            else
            {
                source.AppendLine($"{indent}    {fieldName}.Add(value);");
                source.AppendMarkDirty(indent, markDirtyMethod);
            }
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Remove
            source.AppendLine($"{indent}{propertyAccessor} bool RemoveFrom{propertyName}({elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendReturnIfChanged(indent, $"{fieldName}?.Remove(value) == true", markDirtyMethod);
            source.AppendLine($"{indent}}}");
        }

        if (field.DsIsList)
        {
            source.AppendLine();

            // Insert
            source.AppendLine($"{indent}{propertyAccessor} void InsertInto{propertyName}(int index, {elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLazyCreate(indent, field);
            source.AppendLine($"{indent}    {fieldName}.Insert(index, value);");
            source.AppendMarkDirty(indent, markDirtyMethod);
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // RemoveAt
            source.AppendLine($"{indent}{propertyAccessor} void RemoveFrom{propertyName}At(int index)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    if ({fieldName} != null)");
            source.AppendLine($"{indent}    {{");
            source.AppendLine($"{indent}        {fieldName}.RemoveAt(index);");
            source.AppendMarkDirty($"{indent}    ", markDirtyMethod);
            source.AppendLine($"{indent}    }}");
            source.AppendLine($"{indent}}}");
        }

        source.AppendLine();

        if (field.DsIsArray)
        {
            // Clear
            source.AppendLine($"{indent}{propertyAccessor} void Clear{propertyName}()");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName} = System.Array.Empty<{elementTypeName}>();");
            source.AppendMarkDirty(indent, markDirtyMethod);
            source.AppendLine($"{indent}}}");
        }
        else
        {
            // Clear
            source.AppendLine($"{indent}{propertyAccessor} void Clear{propertyName}()");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    if ({fieldName}?.Count > 0)");
            source.AppendLine($"{indent}    {{");
            source.AppendLine($"{indent}        {fieldName}.Clear();");
            source.AppendMarkDirty($"{indent}    ", markDirtyMethod);
            source.AppendLine($"{indent}    }}");
            source.AppendLine($"{indent}}}");
        }

        return true;
    }
}
