/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EquatableArray.cs                                               *
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ModernUO.Serialization.Generator;

/// <summary>
/// An immutable array with sequence-based value equality, so records holding one participate
/// correctly in incremental pipeline caching.
/// </summary>
public readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> _array = array;

    public int Length => _array.IsDefault ? 0 : _array.Length;

    public bool IsDefaultOrEmpty => _array.IsDefaultOrEmpty;

    public T this[int index] => _array[index];

    public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    public bool Equals(EquatableArray<T> other)
    {
        var a = _array;
        var b = other._array;

        if (a.IsDefaultOrEmpty)
        {
            return b.IsDefaultOrEmpty;
        }

        if (b.IsDefaultOrEmpty || a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] is null ? b[i] is not null : !a[i].Equals(b[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array.IsDefaultOrEmpty)
        {
            return 0;
        }

        unchecked
        {
            var hash = 17;
            for (var i = 0; i < _array.Length; i++)
            {
                hash = hash * 31 + (_array[i]?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class EquatableArray
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) where T : IEquatable<T> =>
        new(source.ToImmutableArray());

    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> source) where T : IEquatable<T> =>
        new(source);
}
