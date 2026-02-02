/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
/// Hints to the source generator that the method should be called when the field with the same order value changes.
/// The method must have the signature: void MethodName(T oldValue, T newValue) where T is the field type.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializableFieldChangedAttribute : Attribute
{
    public int Order { get; }

    public SerializableFieldChangedAttribute(int order) => Order = order;
}
