/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldDefaultAttribute.cs                            *
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

/// <summary>
/// Removed in v4. A default value is declared as the second argument of the field's save
/// flag: <c>[SaveFlag(nameof(ShouldSerializeMethod), nameof(DefaultValueMethod))]</c>, so it
/// cannot exist without one. This conversion does not change the wire format.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[Obsolete("Removed in v4. Declare [SaveFlag(nameof(ShouldSerializeMethod), nameof(DefaultValueMethod))] on the serializable field instead. The wire format does not change.", true)]
public sealed class SerializableFieldDefaultAttribute : Attribute
{
    public SerializableFieldDefaultAttribute(string fieldName)
    {
    }

    public SerializableFieldDefaultAttribute(int order)
    {
    }
}
