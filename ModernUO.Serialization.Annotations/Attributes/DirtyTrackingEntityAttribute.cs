using System;

namespace Server;

/// <summary>
/// Hints to the source generator that this field or property indicates the ISerializable parent of this embedded class.
/// If this is specified on an ISerializable type, it will be ignored.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DirtyTrackingEntity : Attribute
{
    public DirtyTrackingEntity()
    {
    }
}
