using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that a serializable list, dictionary, or set should be tidied up.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class TidyAttribute : Attribute
{
}
