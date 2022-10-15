/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2022 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Method.cs                                      *
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
using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SourceGeneration
{
    public static void GenerateMethodStart(
        this StringBuilder source, string indent, string methodName, Accessibility accessors, bool isOverride,
        string returnType, ImmutableArray<(ITypeSymbol, string)> parameters
    )
    {
        source.Append($"{indent}{accessors.ToFriendlyString()}{(isOverride ? " override" : " virtual")} {returnType} {methodName}(");
        source.GenerateSignatureArguments(parameters);
        source.AppendLine($")\n{indent}{{");
    }

    public static void GenerateMethodEnd(this StringBuilder source, string indent) => source.AppendLine($"{indent}}}");

    public static void GenerateConstructorStart(
        this StringBuilder source, string indent, string className, Accessibility accessors, ImmutableArray<(ITypeSymbol, string)> parameters,
        ImmutableArray<string>? baseParameters, bool isOverload = false
    )
    {
        source.Append($"{indent}{accessors.ToFriendlyString()} {className}(");
        source.GenerateSignatureArguments(parameters);
        source.Append(')');
        if ((baseParameters?.Length ?? 0) > 0)
        {
            source.AppendFormat(" : {0}(", isOverload ? "this" : "base");
            var baseParametersValue = baseParameters.Value;
            for (int i = 0; i < baseParametersValue.Length; i++)
            {
                source.Append(baseParametersValue[i]);
                if (i < baseParametersValue.Length - 1)
                {
                    source.Append(',');
                }
            }
            source.Append(')');
        }

        source.AppendLine($"\n{indent}{{");
    }
}
