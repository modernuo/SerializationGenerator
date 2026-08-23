/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldSaveFlagAttribute.cs                           *
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
/// Removed in v4. Save flags are declared on the serializable field itself:
/// <c>[SaveFlag(nameof(ShouldSerializeMethod))]</c>. This conversion does not change the
/// wire format.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[Obsolete("Removed in v4. Declare [SaveFlag(nameof(ShouldSerializeMethod))] on the serializable field instead. The wire format does not change.", true)]
public sealed class SerializableFieldSaveFlagAttribute : Attribute
{
    public SerializableFieldSaveFlagAttribute(string fieldName)
    {
    }

    public SerializableFieldSaveFlagAttribute(int order)
    {
    }
}
