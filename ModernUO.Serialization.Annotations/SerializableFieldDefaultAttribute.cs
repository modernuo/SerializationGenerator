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
/// Hints to the source generator that the named serializable field should use this method's
/// return value while deserializing when the save flag indicates the value was not written.
///
/// Note: This is only used for the current version, not previous versions. Previous versions
/// will always use null or default for that type if it is not deserialized.
/// <code>
/// [SerializableFieldDefault(nameof(_charges))]
/// private int ChargesDefaultValue() => 8;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializableFieldDefaultAttribute : Attribute
{
    public string FieldName { get; }

    public SerializableFieldDefaultAttribute(string fieldName) => FieldName = fieldName;

    [Obsolete("Order-based linkage was removed in v4. Use [SerializableFieldDefault(nameof(_field))], or [SaveFlag(...)] on the field.", true)]
    public SerializableFieldDefaultAttribute(int order)
    {
    }
}
