using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that the field with the same order value should use a save flag.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializableFieldSaveFlagAttribute : Attribute
{
    public int Order { get; }

    public SerializableFieldSaveFlagAttribute(int order) => Order = order;
}
