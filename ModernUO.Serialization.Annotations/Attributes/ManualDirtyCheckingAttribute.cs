using System;

namespace ModernUO.Serialization;

/// <summary>
/// Indicates that the applied class has dirty checking. This is necessary for classes that are not code genned.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ManualDirtyCheckingAttribute : Attribute
{

}
