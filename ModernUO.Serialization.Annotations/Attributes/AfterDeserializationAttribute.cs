using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that this method should be executed after deserializing the object.
/// Method must have no parameters and return void.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AfterDeserializationAttribute : Attribute
{
    /// <summary>
    /// Indicates whether the source generator should execute the method this is attached to immediately, or when
    /// this is set to false, execute it using a Timer Delay.
    ///
    /// Note: Use false when the after deserialization involves deleting objects. This is to prevent corrupted
    /// deserialization by removing an object before it has finished deserializing.
    /// </summary>
    public bool Synchronous { get; set; }

    public AfterDeserializationAttribute(bool synchronous = true) => Synchronous = true;
}
