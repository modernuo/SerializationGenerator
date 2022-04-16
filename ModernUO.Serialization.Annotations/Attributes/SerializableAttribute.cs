using System;

namespace ModernUO.Serialization;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SerializableAttribute : Attribute
{
    public int Version { get; }
    public bool EncodedVersion { get; }

    public SerializableAttribute(int version, bool encodedVersion = true)
    {
        Version = version;
        EncodedVersion = encodedVersion;
    }
}
