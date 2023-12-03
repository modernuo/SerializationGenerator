using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    public static bool GenerateDataStructureMethods(
        this StringBuilder source,
        Compilation compilation,
        string indent,
        IFieldSymbol symbol,
        string propertyAccessor,
        string? markDirtyMethod
    )
    {
        var propertyName = symbol.Name.GetPropertyName();
        var propertyType = symbol.Type;
        var namedTypeSymbol = propertyType as INamedTypeSymbol;

        var elementTypeName = (propertyType as IArrayTypeSymbol)?.ElementType ?? (namedTypeSymbol?.TypeArguments.Length > 0 ? namedTypeSymbol.TypeArguments[0] : null);
        if (elementTypeName == null)
        {
            return false;
        }

        var isArray = propertyType is IArrayTypeSymbol;
        var isDictionary = propertyType.IsDictionaryInterface(compilation);
        var isList = propertyType.IsListInterface(compilation);
        var isCollection = propertyType.IsCollection(compilation);

        if (isDictionary)
        {
            var valueTypeName = namedTypeSymbol!.TypeArguments[1];

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
        else if (isCollection)
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

        if (isList)
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

        if (isArray)
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
