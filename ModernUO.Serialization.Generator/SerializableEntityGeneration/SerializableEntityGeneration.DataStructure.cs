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

    // Marks dirty only when the mutation reports a change. Without a dirty target, just mutates.
    private static void AppendIfChanged(
        this StringBuilder source, string indent, string changed, string? markDirtyMethod, string? statement = null
    )
    {
        if (markDirtyMethod == null)
        {
            source.AppendLine($"{indent}    {statement ?? changed};");
            return;
        }

        source.AppendLine($"{indent}    if ({changed})");
        source.AppendLine($"{indent}    {{");
        source.AppendLine($"{indent}        {markDirtyMethod};");
        source.AppendLine($"{indent}    }}");
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
        if (field.DsIsDictionary)
        {
            var valueTypeName = field.DsValueType;

            // Add
            source.AppendLine($"{indent}{propertyAccessor} void AddTo{propertyName}({elementTypeName} key, {valueTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLazyCreate(indent, field);
            if (field.DsHasTryAdd)
            {
                source.AppendIfChanged(indent, $"{fieldName}.TryAdd(key, value)", markDirtyMethod);
            }
            else
            {
                source.AppendLine($"{indent}    if (!{fieldName}.ContainsKey(key))");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        {fieldName}.Add(key, value);");
                source.AppendMarkDirty($"{indent}    ", markDirtyMethod);
                source.AppendLine($"{indent}    }}");
            }
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Remove
            source.AppendLine($"{indent}{propertyAccessor} void RemoveFrom{propertyName}({elementTypeName} key)");
            source.AppendLine($"{indent}{{");
            source.AppendIfChanged(indent, $"{fieldName}?.Remove(key) == true", markDirtyMethod, $"{fieldName}?.Remove(key)");
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
            source.AppendLine($"{indent}{propertyAccessor} void AddTo{propertyName}({elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLazyCreate(indent, field);
            if (field.DsIsSet)
            {
                source.AppendIfChanged(indent, $"{fieldName}.Add(value)", markDirtyMethod);
            }
            else
            {
                source.AppendLine($"{indent}    {fieldName}.Add(value);");
                source.AppendMarkDirty(indent, markDirtyMethod);
            }
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Remove
            source.AppendLine($"{indent}{propertyAccessor} void RemoveFrom{propertyName}({elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendIfChanged(indent, $"{fieldName}?.Remove(value) == true", markDirtyMethod, $"{fieldName}?.Remove(value)");
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
