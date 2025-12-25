/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Class.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModernUO.Serialization.Generator;

public static partial class SourceGeneration
{
    public static string GetTypeKeyword(this TypeDeclarationSyntax typeNode) =>
        typeNode switch
        {
            RecordDeclarationSyntax { ClassOrStructKeyword.RawKind: not 0 } => "record struct",
            RecordDeclarationSyntax => "record",
            StructDeclarationSyntax => "struct",
            _ => "class"
        };

    extension(INamedTypeSymbol classSymbol)
    {
        public string GetClassNameWithTypeParameters()
        {
            if (!classSymbol.IsGenericType)
            {
                return classSymbol.Name;
            }

            var typeParams = string.Join(", ", classSymbol.TypeParameters.Select(tp => tp.Name));
            return $"{classSymbol.Name}<{typeParams}>";
        }

        public string GetWhereClausesForTypeParameters()
        {
            if (!classSymbol.IsGenericType)
            {
                return "";
            }

            var whereClauses = new StringBuilder();
            foreach (var typeParam in classSymbol.TypeParameters)
            {
                var constraints = new System.Collections.Generic.List<string>();

                if (typeParam.HasReferenceTypeConstraint)
                {
                    constraints.Add("class");
                }
                else if (typeParam.HasValueTypeConstraint)
                {
                    constraints.Add("struct");
                }
                else if (typeParam.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("unmanaged");
                }

                if (typeParam.HasNotNullConstraint)
                {
                    constraints.Add("notnull");
                }

                foreach (var constraintType in typeParam.ConstraintTypes)
                {
                    constraints.Add(constraintType.ToDisplayString());
                }

                if (typeParam.HasConstructorConstraint)
                {
                    constraints.Add("new()");
                }

                if (constraints.Count > 0)
                {
                    whereClauses.Append($" where {typeParam.Name} : {string.Join(", ", constraints)}");
                }
            }

            return whereClauses.ToString();
        }

        public string GetGenericArityName()
        {
            if (!classSymbol.IsGenericType)
            {
                return classSymbol.ToDisplayString();
            }

            // Get the full namespace path and class name with arity notation
            var containingNamespace = classSymbol.ContainingNamespace?.ToDisplayString();
            var arityName = $"{classSymbol.Name}`{classSymbol.Arity}";

            return string.IsNullOrEmpty(containingNamespace) ? arityName : $"{containingNamespace}.{arityName}";
        }
    }

    extension(StringBuilder source)
    {
        public void GenerateClassStart(
            INamedTypeSymbol classSymbol,
            string indent,
            ImmutableArray<ITypeSymbol> interfaces,
            bool isPartial = true,
            string typeKeyword = "class"
        )
        {
            var accessor = classSymbol.DeclaredAccessibility;
            var className = classSymbol.GetClassNameWithTypeParameters();
            var whereClauses = classSymbol.GetWhereClausesForTypeParameters();

            source.Append($"{indent}{accessor.ToFriendlyString()} {(isPartial ? "partial " : "")}{typeKeyword} {className}");
            if (!interfaces.IsEmpty)
            {
                source.Append(" : ");
                for (var i = 0; i < interfaces.Length; i++)
                {
                    source.Append(interfaces[i].ToDisplayString());
                    if (i < interfaces.Length - 1)
                    {
                        source.Append(", ");
                    }
                }
            }

            source.AppendLine($"{whereClauses}\n{indent}{{");
        }

        public void GenerateClassEnd(string indent)
        {
            source.AppendLine($"{indent}}}");
        }
    }
}
