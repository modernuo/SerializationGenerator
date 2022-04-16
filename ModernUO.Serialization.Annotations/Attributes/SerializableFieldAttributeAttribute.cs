using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that this field will need this attribute on the generated property
/// [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
/// -or-
/// [SerializableFieldAttr(typeof(CommandPropertyAttribute), AccessLevel.GameMaster)]
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class SerializableFieldAttrAttribute : Attribute
{
    public string AttributeString { get; }
    public Type AttributeType { get; }
    public object[] Arguments { get; }

    public SerializableFieldAttrAttribute(string attrString) => AttributeString = attrString;

    public SerializableFieldAttrAttribute(Type type, params object[] args)
    {
        if (!typeof(Attribute).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Argument {nameof(type)} must be an attribute.");
        }

        AttributeType = type;
        Arguments = args;
    }
}
