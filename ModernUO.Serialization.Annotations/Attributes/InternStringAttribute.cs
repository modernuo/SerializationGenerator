using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that a serializable string field or property should be internalized on deserialization
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InternStringAttribute : Attribute
{
}
