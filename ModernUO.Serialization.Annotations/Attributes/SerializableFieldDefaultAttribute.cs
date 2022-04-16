using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that the field with the same order should use this default value
/// while deserializing. The default is used when the save flag indicates that we don't need to serialize the value
/// because this default can be used instead.
///
/// Note: This is only used for the current version, not previous versions. Previous versions will always use null or default
/// for that type if it is not deserialized.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializableFieldDefaultAttribute : Attribute
{
    public int Order { get; }

    public SerializableFieldDefaultAttribute(int order) => Order = order;
}
