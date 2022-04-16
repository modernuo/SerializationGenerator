using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that a serializable int field or property should be encoded
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EncodedIntAttribute : Attribute
{
}
