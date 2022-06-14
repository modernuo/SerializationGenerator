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

    public static bool IsAttributedSyntaxNode<T>(this SyntaxNode node, string attributeName, CancellationToken token)
        where T : MemberDeclarationSyntax
    {
        token.ThrowIfCancellationRequested();

        if (node is AttributeSyntax { Parent.Parent: T } attributeSyntax &&
            (attributeSyntax.Parent.Parent is not ClassDeclarationSyntax classSyntax || classSyntax.IsPartial()))
        {
            var name = attributeSyntax.Name.ExtractName();
            return name == attributeName || name == $"{attributeName}Attribute";
        }

        return false;
    }

    private static string? ExtractName(this NameSyntax? name) =>
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
}
