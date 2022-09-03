/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
        ruleArguments = symbol switch
        {
            _ when symbol.IsPoison(compilation)      => new[] { "Poison" },
            _ when symbol.IsPoint2D(compilation)     => new[] { "Point2D" },
            _ when symbol.IsPoint3D(compilation)     => new[] { "Point3D" },
            _ when symbol.IsRectangle2D(compilation) => new[] { "Rect2D" },
            _ when symbol.IsRectangle3D(compilation) => new[] { "Rect3D" },
            _ when symbol.IsRace(compilation)        => new[] { "Race" },
            _ when symbol.IsMap(compilation)         => new[] { "Map" },
            _ when symbol.IsBitArray(compilation)    => new[] { "BitArray" },
            _ when symbol.IsSerial(compilation)      => new[] { "Serial" },
            _                                        => null
        };

        return ruleArguments != null;
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

        var propertyName = property.FieldName ?? property.Name;
        source.AppendLine($"{indent}{propertyName} = reader.Read{property.RuleArguments?[0] ?? ""}();");
    }

    public override void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        source.AppendLine($"{indent}writer.Write({property.FieldName ?? property.Name});");
    }
}
