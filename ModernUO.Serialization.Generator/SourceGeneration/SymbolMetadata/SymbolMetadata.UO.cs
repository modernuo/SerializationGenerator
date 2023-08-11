/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SymbolMetadata.UO.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SymbolMetadata
{
    public const string INVALIDATE_PROPERTIES_ATTRIBUTE = "ModernUO.Serialization.InvalidatePropertiesAttribute";
    public const string AFTER_DESERIALIZATION_ATTRIBUTE = "ModernUO.Serialization.AfterDeserializationAttribute";
    public const string SERIALIZABLE_ATTRIBUTE = "ModernUO.Serialization.SerializationGeneratorAttribute";
    public const string DIRTY_TRACKING_ENTITY_ATTRIBUTE = "ModernUO.Serialization.DirtyTrackingEntityAttribute";
    public const string SERIALIZABLE_FIELD_ATTRIBUTE = "ModernUO.Serialization.SerializableFieldAttribute";
    public const string SERIALIZABLE_PROPERTY_ATTRIBUTE = "ModernUO.Serialization.SerializablePropertyAttribute";
    public const string DELTA_DATE_TIME_ATTRIBUTE = "ModernUO.Serialization.DeltaDateTimeAttribute";
    public const string INTERN_STRING_ATTRIBUTE = "ModernUO.Serialization.InternStringAttribute";
    public const string ENCODED_INT_ATTRIBUTE = "ModernUO.Serialization.EncodedIntAttribute";
    public const string CAN_BE_NULL_ATTRIBUTE = "ModernUO.Serialization.CanBeNullAttribute";
    public const string TIDY_ATTRIBUTE = "ModernUO.Serialization.TidyAttribute";
    public const string TIMER_DRIFT_ATTRIBUTE = "ModernUO.Serialization.TimerDriftAttribute";
    public const string DESERIALIZE_TIMER_FIELD_ATTRIBUTE = "ModernUO.Serialization.DeserializeTimerFieldAttribute";
    public const string SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE = "ModernUO.Serialization.SerializableFieldSaveFlagAttribute";
    public const string SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE = "ModernUO.Serialization.SerializableFieldDefaultAttribute";
    public const string SERIALIZED_PROPERTY_ATTR_ATTRIBUTE = "ModernUO.Serialization.SerializedPropertyAttrAttribute`1";

    public const string SERIALIZABLE_INTERFACE = "Server.ISerializable";
    public const string GENERIC_WRITER_INTERFACE = "Server.IGenericWriter";
    public const string GENERIC_READER_INTERFACE = "Server.IGenericReader";
    public const string TEXTDEFINITION_CLASS = "Server.TextDefinition";
    public const string POISON_CLASS = "Server.Poison";
    public const string POINT2D_STRUCT = "Server.Point2D";
    public const string POINT3D_STRUCT = "Server.Point3D";
    public const string RECTANGLE2D_STRUCT = "Server.Rectangle2D";
    public const string RECTANGLE3D_STRUCT = "Server.Rectangle3D";
    public const string RACE_CLASS = "Server.Race";
    public const string MAP_CLASS = "Server.Map";
    public const string TIMER_CLASS = "Server.Timer";
    public const string SERIAL_STRUCT = "Server.Serial";
    // ModernUO modified BitArray
    public const string SERVER_BITARRAY_CLASS = "Server.Collections.BitArray";

    public static bool IsSerializedPropertyAttr(this AttributeData attr, Compilation compilation, out ITypeSymbol? genericType)
    {
        var attrClass = attr?.AttributeClass;
        if (attrClass?.BaseType == null || attrClass.BaseType.IsUnboundGenericType || !attrClass.BaseType.IsGenericType)
        {
            genericType = null;
            return false;
        }

        var serializedPropertyAttrType = compilation.GetTypeByMetadataName(SERIALIZED_PROPERTY_ATTR_ATTRIBUTE);
        if (!attrClass.BaseType.ConstructedFrom.Equals(serializedPropertyAttrType, SymbolEqualityComparer.Default))
        {
            genericType = null;
            return false;
        }

        var types = attrClass.BaseType.TypeArguments;
        if (types.Length != 1)
        {
            genericType = null;
            return false;
        }

        genericType = types[0];
        return true;
    }

    public static bool IsCanBeNull(this AttributeData attr, Compilation compilation) =>
        attr?.IsAttribute(compilation.GetTypeByMetadataName(CAN_BE_NULL_ATTRIBUTE)) == true;

    public static bool IsTimerDrift(this AttributeData attr, Compilation compilation) =>
        attr?.IsAttribute(compilation.GetTypeByMetadataName(TIMER_DRIFT_ATTRIBUTE)) == true;

    public static bool IsTimer(this ITypeSymbol symbol, Compilation compilation) =>
        symbol.CanBeConstructedFrom(compilation.GetTypeByMetadataName(TIMER_CLASS));

    public static bool IsEncodedInt(this AttributeData attr, Compilation compilation) =>
        attr?.IsAttribute(compilation.GetTypeByMetadataName(ENCODED_INT_ATTRIBUTE)) == true;

    public static bool IsDeltaDateTime(this AttributeData attr, Compilation compilation) =>
        attr?.IsAttribute(compilation.GetTypeByMetadataName(DELTA_DATE_TIME_ATTRIBUTE)) == true;

    public static bool IsInternString(this AttributeData attr, Compilation compilation) =>
        attr?.IsAttribute(compilation.GetTypeByMetadataName(INTERN_STRING_ATTRIBUTE)) == true;

    public static bool IsTidy(this AttributeData attr, Compilation compilation) =>
        attr?.IsAttribute(compilation.GetTypeByMetadataName(TIDY_ATTRIBUTE)) == true;

    public static bool IsAttribute(this AttributeData attr, ISymbol symbol) =>
        attr?.AttributeClass?.Equals(symbol, SymbolEqualityComparer.Default) == true;

    public static bool IsEnum(this ITypeSymbol symbol) =>
        symbol.SpecialType == SpecialType.System_Enum || symbol.TypeKind == TypeKind.Enum;

    public static bool HasSerializableInterface(
        this ITypeSymbol symbol,
        Compilation compilation
    ) => symbol.ContainsInterface(compilation.GetTypeByMetadataName(SERIALIZABLE_INTERFACE));

    public static bool Contains(this ImmutableArray<INamedTypeSymbol> symbols, ITypeSymbol? symbol) =>
        symbol is INamedTypeSymbol namedSymbol &&
        symbols.Contains(namedSymbol, SymbolEqualityComparer.Default) || symbols.Contains(symbol?.BaseType);

    public static bool HasSerialCtor(
        this INamedTypeSymbol symbol,
        Compilation compilation
    )
    {
        var serialType = compilation.GetTypeByMetadataName(SERIAL_STRUCT);
        return symbol.Constructors.FirstOrDefault(
            member =>
                member.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(member.Parameters[0].Type, serialType)
        ) != null;
    }

    public static bool TryGetEmptyOrParentCtor(
        this INamedTypeSymbol symbol,
        INamedTypeSymbol? parentSymbol,
        out bool requiresParent
    )
    {
        var genericCtor = symbol.Constructors.FirstOrDefault(
            m =>
            {
                if (m.IsStatic || m.MethodKind != MethodKind.Constructor)
                {
                    return false;
                }

                if (m.Parameters.Length == 0)
                {
                    return true;
                }

                if (parentSymbol == null)
                {
                    return false;
                }

                var argType = m.Parameters[0].Type;
                if (argType.TypeKind == TypeKind.Interface)
                {
                    return parentSymbol.Equals(argType, SymbolEqualityComparer.Default) || parentSymbol.ContainsInterface(argType);
                }

                if (parentSymbol?.CanBeConstructedFrom(m.Parameters[0].Type) != true)
                {
                    return false;
                }

                for (var i = 1; i < m.Parameters.Length; i++)
                {
                    if (!m.Parameters[i].IsOptional)
                    {
                        return false;
                    }
                }

                return true;
            }
        );

        requiresParent = genericCtor?.Parameters.Length == 1;
        return genericCtor != null;
    }

    public static bool HasGenericReaderCtor(
        this INamedTypeSymbol symbol,
        Compilation compilation,
        ISymbol? parentSymbol,
        out bool requiresParent
    )
    {
        var genericReaderInterface = compilation.GetTypeByMetadataName(GENERIC_READER_INTERFACE);
        var genericCtor = symbol.Constructors.FirstOrDefault(
            m => !m.IsStatic &&
                 m.MethodKind == MethodKind.Constructor &&
                 m.Parameters.Length >= 1 &&
                 m.Parameters.Length <= 2 &&
                 SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, genericReaderInterface)
        );

        requiresParent = genericCtor?.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(genericCtor.Parameters[1].Type, parentSymbol);
        return genericCtor != null;
    }

    // Note: Does not detect extension methods
    public static bool HasMarkDirtyMethod(this ITypeSymbol symbol)
    {
        return symbol.GetAllMethods("MarkDirty")
            .Any(
                m => !m.IsStatic &&
                     m.ReturnsVoid &&
                     m.Parameters.Length == 0 &&
                     m.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
            );
    }

    public static bool HasPublicSerializeMethod(this ITypeSymbol symbol, Compilation compilation)
    {
        var genericWriterInterface = compilation.GetTypeByMetadataName(GENERIC_WRITER_INTERFACE);

        return symbol.GetAllMethods("Serialize")
            .Any(
                m => !m.IsStatic &&
                     m.ReturnsVoid &&
                     m.Parameters.Length == 1 &&
                     SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, genericWriterInterface) &&
                     m.DeclaredAccessibility == Accessibility.Public
            );
    }

    public static bool HasPublicDeserializeMethod(
        this ITypeSymbol symbol,
        Compilation compilation
    )
    {
        var genericReaderInterface = compilation.GetTypeByMetadataName(GENERIC_READER_INTERFACE);

        return symbol.GetAllMethods("Deserialize")
            .Any(
                m => !m.IsStatic &&
                     m.ReturnsVoid &&
                     m.Parameters.Length == 1 &&
                     SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, genericReaderInterface) &&
                     m.DeclaredAccessibility == Accessibility.Public
            );
    }

    public static bool IsTextDefinition(this ISymbol symbol, Compilation compilation) =>
        symbol.IsTypeRecurse(compilation, compilation.GetTypeByMetadataName(TEXTDEFINITION_CLASS));

    public static bool IsPoison(this ISymbol symbol, Compilation compilation) =>
        symbol.IsTypeRecurse(compilation, compilation.GetTypeByMetadataName(POISON_CLASS));

    public static bool IsPoint2D(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(POINT2D_STRUCT),
            SymbolEqualityComparer.Default
        );

    public static bool IsPoint3D(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(POINT3D_STRUCT),
            SymbolEqualityComparer.Default
        );

    public static bool IsRectangle2D(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(RECTANGLE2D_STRUCT),
            SymbolEqualityComparer.Default
        );

    public static bool IsRectangle3D(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(RECTANGLE3D_STRUCT),
            SymbolEqualityComparer.Default
        );

    public static bool IsRace(this ISymbol symbol, Compilation compilation) =>
        symbol.IsTypeRecurse(compilation, compilation.GetTypeByMetadataName(RACE_CLASS));

    public static bool IsMap(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(MAP_CLASS),
            SymbolEqualityComparer.Default
        );

    public static bool IsBitArray(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(SERVER_BITARRAY_CLASS),
            SymbolEqualityComparer.Default
        );

    public static bool IsSerial(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(SERIAL_STRUCT),
            SymbolEqualityComparer.Default
        );

    public static AttributeData? GetAttribute(this ISymbol symbol, ISymbol attrSymbol) =>
        symbol
            .GetAttributes()
            .FirstOrDefault(
                ad => ad.AttributeClass != null && SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attrSymbol)
            );

    public static bool HasSerializableInterface(this INamedTypeSymbol classSymbol, Compilation compilation) =>
        classSymbol.ContainsInterface(compilation.GetTypeByMetadataName(SERIALIZABLE_INTERFACE));

    public static bool IsSerializableRecursive(this INamedTypeSymbol classSymbol, Compilation compilation)
    {
        while (classSymbol != null)
        {
            if (classSymbol.TryGetSerializable(compilation, out _))
            {
                return true;
            }

            classSymbol = classSymbol.BaseType;
        }

        return false;
    }

    public static bool TryGetSerializable(
        this INamedTypeSymbol classSymbol, Compilation compilation, out AttributeData? attributeData
    )
    {
        var serializableEntityAttribute =
            compilation.GetTypeByMetadataName(SERIALIZABLE_ATTRIBUTE);

        attributeData = classSymbol.GetAttribute(serializableEntityAttribute);
        return attributeData != null;
    }

    public static bool TryGetMemberWithAttribute(
        this ISymbol symbol, INamedTypeSymbol attributeSymbol, out AttributeData? attributeData
    )
    {
        attributeData = symbol.GetAttribute(attributeSymbol);
        return attributeData != null;
    }

    public static bool TryGetSerializableField(
        this ISymbol fieldSymbol, Compilation compilation, out AttributeData? attributeData
    ) => fieldSymbol.TryGetMemberWithAttribute(
        compilation.GetTypeByMetadataName(SERIALIZABLE_FIELD_ATTRIBUTE),
        out attributeData
    );

    public static bool TryGetSerializableProperty(
        this ISymbol propertySymbol, Compilation compilation, out AttributeData? attributeData
    ) => propertySymbol.TryGetMemberWithAttribute(
        compilation.GetTypeByMetadataName(SERIALIZABLE_PROPERTY_ATTRIBUTE),
        out attributeData
    );

    public static bool TryGetDirtyTrackingEntityField(this ISymbol fieldSymbol, Compilation compilation) =>
        fieldSymbol.TryGetMemberWithAttribute(
            compilation.GetTypeByMetadataName(DIRTY_TRACKING_ENTITY_ATTRIBUTE),
            out _
        );

    public static bool TryGetSerializableFieldSaveFlagMethod(
        this ISymbol fieldSymbol, Compilation compilation, out AttributeData? attributeData
    ) => fieldSymbol.TryGetMemberWithAttribute(
        compilation.GetTypeByMetadataName(SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE),
        out attributeData
    );

    public static bool TryGetSerializableFieldDefaultMethod(
        this ISymbol fieldSymbol, Compilation compilation, out AttributeData? attributeData
    ) => fieldSymbol.TryGetMemberWithAttribute(
        compilation.GetTypeByMetadataName(SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE),
        out attributeData
    );
}
