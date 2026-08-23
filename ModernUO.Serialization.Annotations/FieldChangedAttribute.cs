/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FieldChangedAttribute.cs                                        *
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
/// Declares the change callback for a serializable field, on the field itself. The named
/// method must have the signature <c>void Method(T oldValue, T newValue)</c> where T is the
/// field's type; it is invoked by the generated setter after assignment.
/// <code>
/// [SerializableField(2)]
/// [FieldChanged(nameof(OnLevelChanged))]
/// private int _level;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FieldChangedAttribute : Attribute
{
    public string MethodName { get; }

    public FieldChangedAttribute(string methodName) => MethodName = methodName;
}
