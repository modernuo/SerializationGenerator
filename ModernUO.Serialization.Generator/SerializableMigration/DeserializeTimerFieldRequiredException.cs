using System;

namespace ModernUO.Serialization.Generator;

public class DeserializeTimerFieldRequiredException : Exception
{
    public string PropertyName { get; }

    public DeserializeTimerFieldRequiredException(string propertyName) => PropertyName = propertyName;
}
