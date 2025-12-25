/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SortedSetComparerAttribute.cs                                   *
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
/// Specifies a custom comparer to use when deserializing a SortedSet.
/// </summary>
/// <remarks>
/// Usage examples:
/// <code>
/// // Instance comparer - creates new instance: new MyComparer()
/// [SerializableField(0)]
/// [SortedSetComparer(typeof(MyComparer))]
/// private SortedSet&lt;MyType&gt; _items;
///
/// // Static member comparer - uses static property: StringComparer.OrdinalIgnoreCase
/// [SerializableField(0)]
/// [SortedSetComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
/// private SortedSet&lt;string&gt; _names;
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SortedSetComparerAttribute : Attribute
{
    /// <summary>
    /// The type that provides the comparer.
    /// </summary>
    public Type ComparerType { get; }

    /// <summary>
    /// Optional static member (property or field) on <see cref="ComparerType"/> that returns the comparer instance.
    /// If null, a new instance of <see cref="ComparerType"/> will be created.
    /// </summary>
    public string? StaticMember { get; }

    /// <summary>
    /// Creates a SortedSetComparer attribute that instantiates the comparer type.
    /// </summary>
    /// <param name="comparerType">The comparer type to instantiate.</param>
    public SortedSetComparerAttribute(Type comparerType)
    {
        ComparerType = comparerType;
        StaticMember = null;
    }

    /// <summary>
    /// Creates a SortedSetComparer attribute that uses a static member from the comparer type.
    /// </summary>
    /// <param name="comparerType">The type containing the static member.</param>
    /// <param name="staticMember">The name of the static property or field.</param>
    public SortedSetComparerAttribute(Type comparerType, string staticMember)
    {
        ComparerType = comparerType;
        StaticMember = staticMember;
    }
}
