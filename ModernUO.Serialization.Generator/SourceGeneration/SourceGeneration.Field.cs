/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Field.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SourceGeneration
{
    public static string GetFieldName(this string propertyName) => $"_{propertyName.Humanize().Camelize()}";

    public static void GenerateField(
        this StringBuilder source,
        string indent,
        Accessibility accessors,
        InstanceModifier instance,
        string type,
        string variableName,
        string value = null
    )
    {
        var instanceStr = instance == InstanceModifier.None ? "" : $"{instance.ToFriendlyString()} ";
        var accessorStr = accessors == Accessibility.NotApplicable ? "" : $"{accessors.ToFriendlyString()} ";
        var valueStr = value == null ? "" : $" = {value}";
        source.AppendLine($"{indent}{accessorStr}{instanceStr}{type} {variableName}{valueStr};");
    }
}
