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
using SerializationGenerator;

namespace SerializableMigration;

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
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            ruleArguments = null;
            return false;
        }

        if (!namedTypeSymbol.TryGetEmptyOrParentCtor(parentSymbol as INamedTypeSymbol, out var requiresParent))
        {
            ruleArguments = null;
            return false;
        }

        if (namedTypeSymbol.HasSerializableInterface(compilation))
        {
            ruleArguments = null;
            return false;
        }

        if (
            !namedTypeSymbol.HasPublicSerializeMethod(compilation) &&
            !namedTypeSymbol.HasPublicDeserializeMethod(compilation) &&
            !namedTypeSymbol.IsSerializableRecursive(compilation)
        )
        {
            ruleArguments = null;
            return false;
        }

        ruleArguments = new[] { requiresParent ? "DeserializationRequiresParent" : "" };
        return true;
    }

    public override void GenerateDeserializationMethod(
        StringBuilder source, string indent, SerializableProperty property, string? parentReference, bool isMigration = false
    )
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var propertyName = property.Name;
        var argument = property.RuleArguments?.Length >= 1 &&
                       property.RuleArguments[0] == "DeserializationRequiresParent" ? ", this" : "";

        source.AppendLine($"{indent}{propertyName} = new {property.Type}({argument});");
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

        var propertyName = property.Name;
        source.AppendLine($"{indent}{propertyName}.Serialize(writer);");
    }
}
