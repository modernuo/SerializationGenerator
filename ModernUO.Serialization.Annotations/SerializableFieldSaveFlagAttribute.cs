/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
/// Hints to the source generator that the field with the same order value should use a save flag.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializableFieldSaveFlagAttribute : Attribute
{
    public int Order { get; }

    public SerializableFieldSaveFlagAttribute(int order) => Order = order;
}
