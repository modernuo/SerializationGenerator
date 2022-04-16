using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that the specified serializable field, which must be a timer,
/// can be deserialized by this method. The method signature should look like this:
///
/// [DeserializeTimerField(0)]
/// private void DeserializeTimer(TimeSpan delay)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeserializeTimerFieldAttribute : Attribute
{
    public int Order { get; }

    public DeserializeTimerFieldAttribute(int order) => Order = order;
}
