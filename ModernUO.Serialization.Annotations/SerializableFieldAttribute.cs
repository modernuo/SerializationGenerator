/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldAttribute.cs                                   *
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
/// Hints to the source generator that this field should be serialized.
/// The source generator will generate the property entirely
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SerializableFieldAttribute : Attribute
{
    public int Order { get; }
    public string PropertyGetter { get; }
    public string? PropertySetter { get; }
    public bool IsVirtual { get; }

    public SerializableFieldAttribute(
        int order,
        string getter = "public",
        string setter = "public",
        bool isVirtual = false
    )
    {
        Order = order;
        PropertyGetter = getter;
        PropertySetter = setter;
        IsVirtual = isVirtual;
    }
}
