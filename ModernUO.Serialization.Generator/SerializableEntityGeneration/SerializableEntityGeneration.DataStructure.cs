using System.Text;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
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
        var elementTypeName = field.DsElementType;

        if (field.DsIsDictionary)
        {
            var valueTypeName = field.DsValueType;

            // Add
            source.AppendLine($"{indent}{propertyAccessor} void AddTo{propertyName}({elementTypeName} key, {valueTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.Add(key, value);");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Remove
            source.AppendLine($"{indent}{propertyAccessor} void RemoveFrom{propertyName}({elementTypeName} key)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.Remove(key);");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Replace
            source.AppendLine($"{indent}{propertyAccessor} void ReplaceIn{propertyName}({elementTypeName} key, {valueTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}[key] = value;");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");
        }
        else if (field.DsIsCollection)
        {
            // Add
            source.AppendLine($"{indent}{propertyAccessor} void AddTo{propertyName}({elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.Add(value);");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // Remove
            source.AppendLine($"{indent}{propertyAccessor} void RemoveFrom{propertyName}({elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.Remove(value);");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");

            source.AppendLine();
        }

        if (field.DsIsList)
        {
            // Insert
            source.AppendLine($"{indent}{propertyAccessor} void InsertInto{propertyName}(int index, {elementTypeName} value)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.Insert(index, value);");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");

            source.AppendLine();

            // RemoveAt
            source.AppendLine($"{indent}{propertyAccessor} void RemoveFrom{propertyName}At(int index)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.RemoveAt(index);");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");
        }

        source.AppendLine();

        if (field.DsIsArray)
        {
            // Clear
            source.AppendLine($"{indent}{propertyAccessor} void Clear{propertyName}()");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName} = System.Array.Empty<{elementTypeName}>();");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            // Clear
            source.AppendLine($"{indent}{propertyAccessor} void Clear{propertyName}()");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName}.Clear();");
            source.AppendLine($"{indent}    {markDirtyMethod};");
            source.AppendLine($"{indent}}}");
        }

        return true;
    }
}
