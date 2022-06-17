using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public record SerializableClassRecord(
    INamedTypeSymbol ClassSymbol,
    AttributeData SerializationAttribute,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFieldsAndProperties,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFieldSaveFlags,
    ImmutableArray<(ISymbol, AttributeData)> SerializableFieldDefault,
    ISymbol? DirtyTrackingEntity,
    ImmutableDictionary<int, AdditionalText> migrations
);
