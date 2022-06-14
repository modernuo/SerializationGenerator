using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public record SerializableClassRecord(
    INamedTypeSymbol classSymbol,
    AttributeData serializationAttribute,
    ImmutableArray<(ISymbol, AttributeData)> fieldsAndProperties,
    ISymbol? dirtyTrackingEntity,
    ImmutableDictionary<int, AdditionalText> migrations
);
