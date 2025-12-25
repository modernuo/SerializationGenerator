/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Enum.cs                                        *
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
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SourceGeneration
{
    extension(StringBuilder source)
    {
        public void GenerateEnumStart(
            string enumName,
            string indent,
            bool useFlags,
            Accessibility accessor = Accessibility.Public,
            string underlyingType = null
        )
        {
            if (useFlags)
            {
                source.AppendLine($"{indent}[System.Flags]");
            }

            var typeSpecifier = underlyingType != null ? $" : {underlyingType}" : "";
            source.AppendLine($"{indent}{accessor.ToFriendlyString()} enum {enumName}{typeSpecifier}\n{indent}{{");
        }

        public void GenerateEnumValue(string indent, bool isFlag, string name, int value)
        {
            var number = value < 0 ? 0 : 1 << value;
            var valueStr = isFlag ? $"0x{number:X8}" : value.ToString();
            source.AppendLine($"{indent}{name} = {valueStr},");
        }

        public void GenerateEnumValueLong(string indent, bool isFlag, string name, int value)
        {
            var number = value < 0 ? 0UL : 1UL << value;
            var valueStr = isFlag ? $"0x{number:X16}" : value.ToString();
            source.AppendLine($"{indent}{name} = {valueStr},");
        }

        public void GenerateEnumEnd(string indent)
        {
            source.AppendLine($"{indent}}}");
        }
    }
}
