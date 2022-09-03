/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializablePropertyAttribute.cs                                *
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
/// Hints to the source generator that this property should be serialized.
/// If useField is null or unspecified, the source generator will create a private backing field for you.
/// Note: The user must call this.MarkDirty() after reassigning the value to the backing field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Property)]
public sealed class SerializablePropertyAttribute : Attribute
{
    public int Order { get; }
    public string UseField { get; }

    public SerializablePropertyAttribute(
        int order,
        string useField = null
    )
    {
        Order = order;
        UseField = useField;
    }
}
