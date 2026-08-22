/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AnchoredDateTimeAttribute.cs                                    *
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
/// Hints to the source generator that a serializable DateTime field or property is anchored to
/// the save time: it is written as an absolute value and re-anchored once at load, so downtime
/// does not age it and an unchanged entity serializes to identical bytes. Takes precedence
/// over <see cref="DeltaDateTimeAttribute" /> when both are present.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AnchoredDateTimeAttribute : Attribute
{
}
