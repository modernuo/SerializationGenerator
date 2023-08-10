/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PrimitiveUOTypeMigrationRule.cs                                 *
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
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public class PrimitiveUOTypeMigrationRule : MigrationRule
{
    public override string RuleName => nameof(PrimitiveUOTypeMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        var type = symbol switch
        {
            _ when symbol.IsTextDefinition(compilation) => "TextDefinition",
            _ when symbol.IsPoison(compilation)         => "Poison",
            _ when symbol.IsPoint2D(compilation)        => "Point2D",
            _ when symbol.IsPoint3D(compilation)        => "Point3D",
            _ when symbol.IsRectangle2D(compilation)    => "Rect2D",
            _ when symbol.IsRectangle3D(compilation)    => "Rect3D",
            _ when symbol.IsRace(compilation)           => "Race",
            _ when symbol.IsMap(compilation)            => "Map",
            _ when symbol.IsBitArray(compilation)       => "BitArray",
            _ when symbol.IsSerial(compilation)         => "Serial",
            _                                           => null
        };

        if (type == null)
        {
            ruleArguments = null;
            return false;
        }

        var canBeNull = attributes.Any(a => a.IsCanBeNull(compilation));
        ruleArguments = canBeNull ? new[] { "@CanBeNull", type } : new[] { type };
        return true;
    }

    public override void GenerateDeserializationMethod(
        StringBuilder source,
        string indent,
        Compilation compilation,
        SerializableProperty property,
        string? parentReference,
        bool isMigration = false
    )
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var ruleArguments = property.RuleArguments;
        var canBeNull = ruleArguments!.Length > 0 && ruleArguments[0] == "@CanBeNull";
        var index = canBeNull ? 1 : 0;

        var type = ruleArguments[index];
        var propertyName = property.FieldName ?? property.Name;

        if (canBeNull)
        {
            source.AppendLine($"{indent}if (reader.ReadBool())");
            source.AppendLine($"{indent}{{");
            GenerateDeserialize(source, $"{indent}    ", propertyName, type);
            source.AppendLine($"{indent}}}");
            source.AppendLine($"{indent}else");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName} = default;");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            GenerateDeserialize(source, indent, propertyName, type);
        }
    }

    private static void GenerateDeserialize(
        StringBuilder source,
        string indent,
        string propertyName,
        string? type
    )
    {
        source.AppendLine($"{indent}{propertyName} = reader.Read{type ?? ""}();");
    }

    public override void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var ruleArguments = property.RuleArguments;
        var canBeNull = ruleArguments!.Length > 0 && ruleArguments[0] == "@CanBeNull";

        var propertyName = property.FieldName ?? property.Name;

        if (canBeNull)
        {
            var newIndent = $"{indent}    ";
            source.AppendLine($"{indent}if ({propertyName} != default)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{newIndent}writer.Write(true);");
            source.AppendLine($"{newIndent}writer.Write({propertyName});");
            source.AppendLine($"{indent}}}");
            source.AppendLine($"{indent}else");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{newIndent}writer.Write(false);");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            source.AppendLine($"{indent}writer.Write({propertyName});");
        }
    }
}
