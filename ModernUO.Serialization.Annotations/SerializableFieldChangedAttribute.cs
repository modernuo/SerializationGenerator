/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldChangedAttribute.cs                            *
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
/// Removed in v4. Change callbacks are declared on the serializable field itself:
/// <c>[FieldChanged(nameof(Method))]</c>. This conversion does not change the wire format.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[Obsolete("Removed in v4. Declare [FieldChanged(nameof(Method))] on the serializable field instead. The wire format does not change.", true)]
public sealed class SerializableFieldChangedAttribute : Attribute
{
    public SerializableFieldChangedAttribute(string fieldName)
    {
    }

    public SerializableFieldChangedAttribute(int order)
    {
    }
}
