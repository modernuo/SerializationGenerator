using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that this serializable timer field or property will drift
/// during deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class TimerDriftAttribute : Attribute
{
}
