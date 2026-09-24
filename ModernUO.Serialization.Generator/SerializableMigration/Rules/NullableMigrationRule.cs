/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NullableMigrationRule.cs                                        *
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

/// <summary>
/// Nullable&lt;T&gt; value types: a HasValue bool, then the value through T's own rule when present.
/// Rule arguments are [T, T's rule, ...T's rule arguments].
/// </summary>
public class NullableMigrationRule : MigrationRule
{
    public override string RuleName => nameof(NullableMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            ruleArguments = null;
            return false;
        }

        var valueType = nullable.TypeArguments[0];

        var serializableValue = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "NullableValue",
            valueType,
            0,
            attributes,
            parentSymbol,
            null
        );

        var length = serializableValue.RuleArguments?.Length ?? 0;
        ruleArguments = new string[2 + length];
        ruleArguments[0] = valueType.ToSerializedTypeName();
        ruleArguments[1] = serializableValue.Rule;

        if (length > 0)
        {
            Array.Copy(serializableValue.RuleArguments!, 0, ruleArguments, 2, length);
        }

        return true;
    }

    public override void GenerateDeserializationMethod(
        StringBuilder source,
        string indent,
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
        var valueName = $"{propertyName}Value";
        var valueProperty = GetValueProperty(property, valueName, out var valueRule);

        source.AppendLine($"{indent}if (reader.ReadBool())");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    {valueProperty.Type} {valueName};");
        valueRule.GenerateDeserializationMethod(source, $"{indent}    ", valueProperty, parentReference);
        source.AppendLine($"{indent}    {propertyName} = {valueName};");
        source.AppendLine($"{indent}}}");
        source.AppendLine($"{indent}else");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    {propertyName} = null;");
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

        var propertyName = property.FieldName ?? property.Name;
        var valueName = $"{propertyName}Value";
        var valueProperty = GetValueProperty(property, valueName, out var valueRule);

        source.AppendLine($"{indent}writer.Write({propertyName}.HasValue);");
        source.AppendLine($"{indent}if ({propertyName}.HasValue)");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    var {valueName} = {propertyName}.Value;");
        valueRule.GenerateSerializationMethod(source, $"{indent}    ", valueProperty);
        source.AppendLine($"{indent}}}");
    }

    private static SerializableProperty GetValueProperty(
        SerializableProperty property, string valueName, out ISerializableMigrationRule valueRule
    )
    {
        var ruleArguments = property.RuleArguments!;
        valueRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];

        var valueRuleArguments = new string[ruleArguments.Length - 2];
        Array.Copy(ruleArguments, 2, valueRuleArguments, 0, valueRuleArguments.Length);

        return new SerializableProperty
        {
            Name = valueName,
            Type = ruleArguments[0],
            Rule = valueRule.RuleName,
            RuleArguments = valueRuleArguments.Length > 0 ? valueRuleArguments : null
        };
    }
}
