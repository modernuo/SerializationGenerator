/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ListMigrationRule.cs                                            *
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

public class ListMigrationRule : MigrationRule
{
    public override string RuleName => nameof(ListMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsList(compilation))
        {
            ruleArguments = null;
            return false;
        }

        var listTypeSymbol = namedTypeSymbol.TypeArguments[0];

        var serializableListType = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "ListEntry",
            listTypeSymbol,
            0,
            attributes,
            parentSymbol,
            null
        );

        var extraOptions = "";
        if (attributes.Any(a => a.IsTidy(compilation)))
        {
            extraOptions += "@Tidy";
        }

        var length = serializableListType.RuleArguments?.Length ?? 0;
        ruleArguments = new string[length + 3];
        ruleArguments[0] = extraOptions;
        ruleArguments[1] = listTypeSymbol.ToDisplayString();
        ruleArguments[2] = serializableListType.Rule;

        if (length > 0)
        {
            Array.Copy(serializableListType.RuleArguments!, 0, ruleArguments, 3, length);
        }

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
        var hasExtraOptions = ruleArguments![0] == "" || ruleArguments[0].StartsWith("@", StringComparison.Ordinal);
        var argumentsOffset = hasExtraOptions ? 1 : 0;

        var listElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[argumentsOffset + 1]];

        var listElementRuleArguments = new string[ruleArguments.Length - 2 - argumentsOffset];
        Array.Copy(ruleArguments, 2 + argumentsOffset, listElementRuleArguments, 0, ruleArguments.Length - 2 - argumentsOffset);

        var propertyName = property.FieldName ?? property.Name;
        var propertyIndex = $"{propertyName}Index";
        var propertyEntry = $"{propertyName}Entry";
        var propertyCount = $"{propertyName}Count";

        source.AppendLine($"{indent}{ruleArguments[argumentsOffset]} {propertyEntry};");
        source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
        source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.List<{ruleArguments[argumentsOffset]}>({propertyCount});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableListElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = ruleArguments[argumentsOffset],
            Rule = listElementRule.RuleName,
            RuleArguments = listElementRuleArguments
        };

        listElementRule.GenerateDeserializationMethod(
            source,
            $"{indent}    ",
            compilation,
            serializableListElement,
            parentReference
        );
        source.AppendLine($"{indent}    {propertyName}.Add({propertyEntry});");

        source.AppendLine($"{indent}}}");
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
        var hasExtraOptions = ruleArguments![0] == "" || ruleArguments[0].StartsWith("@", StringComparison.Ordinal);
        var shouldTidy = hasExtraOptions && ruleArguments[0].Contains("@Tidy");
        var argumentsOffset = hasExtraOptions ? 1 : 0;

        var listElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1 + argumentsOffset]];
        var listElementRuleArguments = new string[ruleArguments.Length - 2 - argumentsOffset];
        Array.Copy(ruleArguments, 2 + argumentsOffset, listElementRuleArguments, 0, ruleArguments.Length - 2 - argumentsOffset);

        var propertyName = property.FieldName ?? property.Name;
        var propertyEntry = $"{propertyName}Entry";
        var propertyCount = $"{propertyName}Count";

        if (shouldTidy)
        {
            source.AppendLine($"{indent}{propertyName}?.Tidy();");
        }
        source.AppendLine($"{indent}var {propertyCount} = {propertyName}?.Count ?? 0;");
        source.AppendLine($"{indent}writer.WriteEncodedInt({propertyCount});");
        source.AppendLine($"{indent}if ({propertyCount} > 0)");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    foreach (var {propertyEntry} in {propertyName}!)");
        source.AppendLine($"{indent}    {{");

        var serializableListElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = ruleArguments[argumentsOffset],
            Rule = listElementRule.RuleName,
            RuleArguments = listElementRuleArguments
        };

        listElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableListElement);

        source.AppendLine($"{indent}    }}");
        source.AppendLine($"{indent}}}");
    }
}
