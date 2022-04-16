using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that this field or property should be serialized.
/// When used on a field, the source generator will generate the property entirely.
/// When used on a property, the user must call this.MarkDirty() after reassigning the value or modifying the value internally.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SerializableFieldAttribute : Attribute
{
    public int Order { get; }
    public string PropertyGetter { get; }
    public string? PropertySetter { get; }
    public bool IsVirtual { get; }

    public SerializableFieldAttribute(
        int order,
        string getter = "public",
        string setter = "public",
        bool isVirtual = false
    )
    {
        Order = order;
        PropertyGetter = getter;
        PropertySetter = setter;
        IsVirtual = isVirtual;
    }
}
