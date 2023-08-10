/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Text;
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

    public static string? ExtractName(this NameSyntax? name) =>
        name switch
        {
            SimpleNameSyntax ins    => ins.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            _                       => null
        };

    public static bool IsPartial(this ClassDeclarationSyntax classDeclaration)
    {
        foreach (var m in classDeclaration.Modifiers)
        {
            if (m.IsKind(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }

    public static IncrementalValuesProvider<T> RemoveNulls<T>(this IncrementalValuesProvider<T?> source) where T : class =>
        source
            .Where(t => t != null)
            .Select(
                (t, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return (T)Convert.ChangeType(t, typeof(T));
                }
            );

    public static Diagnostic GenerateDiagnostic(this SyntaxNode node, DiagnosticDescriptor descriptor, params object[] msgParams)
    {
        var location = Location.Create(node.SyntaxTree, node.Span);

        return Diagnostic.Create(descriptor, location, msgParams);
    }

    public static void AggressiveInline(this StringBuilder source, string indent) =>
        source.AppendLine(
            $"{indent}[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]"
        );
}
