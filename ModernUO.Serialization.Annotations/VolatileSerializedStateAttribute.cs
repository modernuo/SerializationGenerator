/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: VolatileSerializedStateAttribute.cs                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace ModernUO.Serialization;

/// <summary>Why a type's serialized bytes can change without any tracked mutation.</summary>
[Flags]
public enum VolatileReason
{
    None = 0,

    /// <summary>
    /// A serialized <c>Timer</c> field: its next tick is what gets written, and it moves when the
    /// timer fires, restarts or is stopped directly on the timer object, none of which passes
    /// through the entity.
    /// </summary>
    SerializedTimer = 1,

    /// <summary>A [DeltaDateTime] field: written relative to the moment of the save.</summary>
    DeltaDateTime = 2,

    /// <summary>Declared by hand, for state the generator cannot see (custom getters, derived data).</summary>
    Declared = 4
}

/// <summary>
/// Marks a type whose serialized bytes are not stable across saves even when nothing marked
/// it dirty. A delta save must always re-serialize such a type rather than trust its dirty flag.
/// The generator emits this automatically for serialized timers and [DeltaDateTime] fields;
/// declare it by hand (the generator then emits nothing) for state it cannot see.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class VolatileSerializedStateAttribute : Attribute
{
    public VolatileReason Reason { get; }

    public VolatileSerializedStateAttribute(VolatileReason reason) => Reason = reason;
}
