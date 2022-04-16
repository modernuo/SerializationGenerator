using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that this field will execute an InvalidateProperties when the value is set
/// and the value is different from the current value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InvalidatePropertiesAttribute : Attribute
{
}
