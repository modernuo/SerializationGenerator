/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SaveFlagAttribute.cs                                            *
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
/// Declares conditional serialization for a serializable field, on the field itself. The
/// first method (<c>bool Method()</c>) decides whether the value is written; the optional
/// second method (returning the field's type, no parameters) supplies the value at load when
/// it was not written.
/// <code>
/// [SerializableField(0)]
/// [SaveFlag(nameof(ShouldSerializeName), nameof(NameDefaultValue))]
/// private string _name;
/// </code>
/// Equivalent to placing [SerializableFieldSaveFlag] and [SerializableFieldDefault] on the
/// methods; declare one style per field, not both.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SaveFlagAttribute : Attribute
{
    public string ShouldSerializeMethod { get; }

    public string DefaultValueMethod { get; }

    public SaveFlagAttribute(string shouldSerializeMethod, string defaultValueMethod = null)
    {
        ShouldSerializeMethod = shouldSerializeMethod;
        DefaultValueMethod = defaultValueMethod;
    }
}
