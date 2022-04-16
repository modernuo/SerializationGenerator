/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DirtyTrackingEntityAttribute.cs                                 *
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
/// Hints to the source generator that this field or property indicates the ISerializable parent of this embedded class.
/// If this is specified on an ISerializable type, it will be ignored.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DirtyTrackingEntityAttribute : Attribute
{
    public DirtyTrackingEntityAttribute()
    {
    }
}
