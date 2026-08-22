/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: KnownTypeCache.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

/// <summary>
/// Per-compilation memoization for <see cref="Compilation.GetTypeByMetadataName" />, which
/// walks assemblies on every call. The transforms resolve the same handful of names per field
/// per class per compilation; this collapses those to dictionary hits. Keyed weakly so
/// discarded compilations do not pin their symbols.
/// </summary>
public static class KnownTypeCache
{
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<string, INamedTypeSymbol>> _cache =
        new();

    public static INamedTypeSymbol GetCachedTypeByMetadataName(this Compilation compilation, string metadataName)
    {
        var types = _cache.GetOrCreateValue(compilation);

        if (!types.TryGetValue(metadataName, out var symbol))
        {
            symbol = compilation.GetTypeByMetadataName(metadataName);
            types.TryAdd(metadataName, symbol);
        }

        return symbol;
    }
}
