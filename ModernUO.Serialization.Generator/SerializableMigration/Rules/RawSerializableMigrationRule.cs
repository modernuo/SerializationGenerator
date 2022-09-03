/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationMethodSignatureMigrationRule.cs                    *
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

public class RawSerializableMigrationRule : MigrationRule
{
    public override string RuleName => nameof(RawSerializableMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        ruleArguments = null;
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return false;
        }

        if (!namedTypeSymbol.TryGetEmptyOrParentCtor(parentSymbol as INamedTypeSymbol, out var requiresParent))
        {
            return false;
        }

        if (namedTypeSymbol.HasSerializableInterface(compilation))
        {
            return false;
        }

        ruleArguments = new[] { requiresParent ? "DeserializationRequiresParent" : "" };
        return namedTypeSymbol.IsSerializableRecursive(compilation);
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

        var propertyType = property.Type;
        var propertyName = property.FieldName ?? property.Name;

        var argument = property.RuleArguments?.Length >= 1 &&
                       property.RuleArguments[0] == "DeserializationRequiresParent" ? parentReference ?? "" : "";

        source.AppendLine($"{indent}{propertyName} = new {propertyType}({argument});");
        source.AppendLine($"{indent}{propertyName}.Deserialize(reader);");
    }

    public override void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        source.AppendLine($"{indent}{property.FieldName ?? property.Name}.Serialize(writer);");
    }
}
