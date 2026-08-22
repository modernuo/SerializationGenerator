using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModernUO.Serialization.Generator;

public record SerializableClassRecord(
    TypeDeclarationSyntax TypeNode,
    INamedTypeSymbol ClassSymbol,
    AttributeData SerializationAttribute,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFields,
    ImmutableArray<(ISymbol, AttributeData)> SerializableProperties,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFieldSaveFlags,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFieldDefault,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFieldChanged,
    ISymbol? DirtyTrackingEntity,
    ImmutableDictionary<int, AdditionalText> Migrations,
    ImmutableArray<string> DuplicateMigrationFiles
)
{
    public bool IsValueType => ClassSymbol.IsValueType;
};
