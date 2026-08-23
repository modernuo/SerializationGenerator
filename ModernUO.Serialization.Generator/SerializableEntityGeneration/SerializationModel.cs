/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationModel.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ModernUO.Serialization.Generator;

/// <summary>
/// Everything code generation needs, fully resolved from symbols at transform time and
/// value-equatable so the incremental pipeline can cache on it. Editing anything that does
/// not affect the serialization surface of a class produces an equal model, and nothing
/// downstream re-runs.
/// </summary>
public sealed record SerializationModel(
    string NamespaceName,
    string ClassName,
    string ClassDisplayString,
    string ArityName,
    string TypeKeyword,
    bool IsValueType,
    int Version,
    bool EncodedVersion,
    bool IsOverride,
    bool SerializeOverride,
    bool IsSerializable,
    bool HasSerialCtor,
    bool EmitMarkDirtyMethod,
    string? MarkDirtyBody,
    string? MarkDirtyMethod,
    string ParentReference,
    EquatableArray<TypeShellModel> TypeShells,
    EquatableArray<BackingFieldModel> BackingFields,
    EquatableArray<SerializableProperty> Fields,
    EquatableArray<FieldPropertyModel> FieldEmissions,
    EquatableArray<SaveFlagModel> SaveFlags,
    EquatableArray<AfterDeserializeModel> AfterDeserialization,
    EquatableArray<TimerFieldModel> TimerFields,
    LocationInfo Location
)
{
    // Location is deliberately excluded: an edit elsewhere in the file shifts the class's
    // span without changing its serialization surface, and must not defeat caching. The
    // trade-off is that a cached model's diagnostic location can lag by a few lines until
    // the surface actually changes.
    public bool Equals(SerializationModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return NamespaceName == other.NamespaceName
               && ClassName == other.ClassName
               && ClassDisplayString == other.ClassDisplayString
               && ArityName == other.ArityName
               && TypeKeyword == other.TypeKeyword
               && IsValueType == other.IsValueType
               && Version == other.Version
               && EncodedVersion == other.EncodedVersion
               && IsOverride == other.IsOverride
               && SerializeOverride == other.SerializeOverride
               && IsSerializable == other.IsSerializable
               && HasSerialCtor == other.HasSerialCtor
               && EmitMarkDirtyMethod == other.EmitMarkDirtyMethod
               && MarkDirtyBody == other.MarkDirtyBody
               && MarkDirtyMethod == other.MarkDirtyMethod
               && ParentReference == other.ParentReference
               && TypeShells == other.TypeShells
               && BackingFields == other.BackingFields
               && Fields == other.Fields
               && FieldEmissions == other.FieldEmissions
               && SaveFlags == other.SaveFlags
               && AfterDeserialization == other.AfterDeserialization
               && TimerFields == other.TimerFields;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (NamespaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + (ClassDisplayString?.GetHashCode() ?? 0);
            hash = hash * 31 + Version;
            hash = hash * 31 + Fields.GetHashCode();
            hash = hash * 31 + FieldEmissions.GetHashCode();
            return hash;
        }
    }
}

/// <summary>One containing type in the nesting chain, outermost first.</summary>
public sealed record TypeShellModel(string Accessibility, string NameWithTypeParameters, string WhereClauses);

/// <summary>A field generated to back a [SerializableProperty] member.</summary>
public sealed record BackingFieldModel(string TypeDisplay, string FieldName);

/// <summary>Emission facts for one [SerializableField] member, in declaration order.</summary>
public sealed record FieldPropertyModel(
    int Order,
    string FieldName,
    string FieldTypeDisplay,
    string PropertyName,
    Accessibility Getter,
    Accessibility? Setter,
    bool IsReadOnly,
    bool Virtual,
    bool HasInequalityOperator,
    bool InvalidateProperties,
    string? FieldChangedMethodName,
    string? AllowFieldChangeMethodName,
    EquatableArray<string> AttributeLines,
    bool DsIsArray,
    bool DsIsDictionary,
    bool DsIsList,
    bool DsIsCollection,
    string? DsElementType,
    string? DsValueType
)
{
    public bool HasDataStructureMethods => DsIsArray || DsIsDictionary || DsIsList || DsIsCollection;
}

/// <summary>Save-flag/default method names for one field order.</summary>
public sealed record SaveFlagModel(int Order, string DetermineName, string? DefaultName);

public sealed record AfterDeserializeModel(string MethodName, bool Synchronous);

/// <summary>The [DeserializeTimerField] method for a timer field.</summary>
public sealed record TimerFieldModel(int Order, string DeserializeMethodName);

/// <summary>Value-equatable stand-in for <see cref="Location" />.</summary>
public sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo Create(Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new LocationInfo(
            location.SourceTree?.FilePath ?? lineSpan.Path ?? "",
            location.SourceSpan,
            lineSpan.Span
        );
    }

    public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
}

/// <summary>Transform output: a model, or the diagnostics explaining why there is none.</summary>
public sealed record SerializationModelResult(
    SerializationModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics
);

/// <summary>A parsed migration file; <see cref="Metadata" /> is null when parsing failed.</summary>
public sealed record MigrationFileModel(
    string ClassName,
    int Version,
    string FilePath,
    SerializableMetadata? Metadata,
    string? Error
);

/// <summary>The model plus its class's migrations, ready for generation.</summary>
public sealed record FinalModel(
    SerializationModelResult Result,
    EquatableArray<SerializableMetadata> Migrations,
    EquatableArray<DiagnosticInfo> MigrationDiagnostics
);
