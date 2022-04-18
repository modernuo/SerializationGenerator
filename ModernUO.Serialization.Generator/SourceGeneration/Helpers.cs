/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Helpers.cs                                                      *
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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModernUO.Serialization.Generator;

public static class Helpers
{
    public static bool ContainsInterface(this ITypeSymbol symbol, ISymbol interfaceSymbol) =>
        symbol.Interfaces.Any(i => i.ConstructedFrom.Equals(interfaceSymbol, SymbolEqualityComparer.Default)) ||
        symbol.AllInterfaces.Any(i => i.ConstructedFrom.Equals(interfaceSymbol, SymbolEqualityComparer.Default));

    public static ImmutableArray<IMethodSymbol> GetAllMethods(this ITypeSymbol symbol, string name)
    {
        var builder = ImmutableArray.CreateBuilder<IMethodSymbol>();
        builder.AddRange(symbol.GetMembers(name).OfType<IMethodSymbol>());
        if (symbol.BaseType is ITypeSymbol typeSymbol)
        {
            builder.AddRange(typeSymbol.GetAllMethods(name));
        }
        return builder.ToImmutable();
    }

    public static string ToFriendlyString(this Accessibility accessibility) => SyntaxFacts.GetText(accessibility);

    public static Accessibility GetAccessibility(string? value) =>
        value switch
        {
            "private"            => Accessibility.Private,
            "protected"          => Accessibility.Protected,
            "internal"           => Accessibility.Internal,
            "public"             => Accessibility.Public,
            "protected internal" => Accessibility.ProtectedOrInternal,
            "private protected"  => Accessibility.ProtectedAndInternal,
            _                    => Accessibility.NotApplicable
        };

    public static bool IsTypeRecurse(this ISymbol symbol, Compilation compilation, INamedTypeSymbol classSymbol)
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return false;
        }

        return namedTypeSymbol.Equals(classSymbol, SymbolEqualityComparer.Default) ||
               namedTypeSymbol.BaseType?.IsPoison(compilation) == true;
    }

    public static bool CanBeConstructedFrom(this ITypeSymbol? symbol, ISymbol classSymbol) =>
        symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.ConstructedFrom.Equals(
            classSymbol,
            SymbolEqualityComparer.Default
        ) || symbol != null && CanBeConstructedFrom(symbol.BaseType, classSymbol);

    public static bool IsSyntaxNode<T>(this SyntaxNode node, CancellationToken token) where T : MemberDeclarationSyntax
    {
        token.ThrowIfCancellationRequested();
        return node is T { AttributeLists.Count: > 0 };
    }

    public static bool IsSyntaxNode<T, V>(this SyntaxNode node, CancellationToken token) where T : MemberDeclarationSyntax where V : MemberDeclarationSyntax
    {
        token.ThrowIfCancellationRequested();
        return node is T { AttributeLists.Count: > 0 } or V { AttributeLists.Count: > 0};
    }

    public static IncrementalValueProvider<ImmutableDictionary<T, V>> ToImmutableDictionary<T, V>(this IncrementalValuesProvider<(T, V)> source) =>
        source
            .Collect()
            .Select(MergeToDictionary);

    public static IncrementalValuesProvider<T> Flatten<T>(this IncrementalValuesProvider<ImmutableArray<T>> source) =>
        source.SelectMany(
            (array, token) =>
            {
                token.ThrowIfCancellationRequested();
                return array;
            }
        );

    public static IncrementalValuesProvider<T> RemoveNulls<T>(this IncrementalValuesProvider<T?> source) where T : struct =>
        source
            .Where(t => t.HasValue)
            .Select(
                (t, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return (T)Convert.ChangeType(t, typeof(T));
                }
            );

    public static IncrementalValuesProvider<T> Merge<T>(
        this IncrementalValuesProvider<T> left, IncrementalValuesProvider<T> right
    ) => left
        .Collect()
        .Combine(right.Collect())
        .SelectMany(SelectMerge);

    private static ImmutableDictionary<T, V> MergeToDictionary<T, V>(ImmutableArray<(T, V)> source, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return source.ToImmutableDictionary(key => key.Item1, key => key.Item2);
    }

    private static ImmutableArray<T> SelectMerge<T>((ImmutableArray<T>, ImmutableArray<T>) combined, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var (left, right) = combined;
        var builder = ImmutableArray.CreateBuilder<T>();
        builder.AddRange(left);
        builder.AddRange(right);

        return builder.ToImmutableArray();
    }
}
