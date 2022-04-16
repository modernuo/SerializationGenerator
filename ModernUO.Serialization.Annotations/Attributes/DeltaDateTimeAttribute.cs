using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that a serializable DateTime field or property is for delta time (duration)
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DeltaDateTimeAttribute : Attribute
{
}
