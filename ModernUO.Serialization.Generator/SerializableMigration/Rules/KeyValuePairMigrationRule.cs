/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: KeyValuePairMigrationRule.cs                                    *
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

public class KeyValuePairMigrationRule : MigrationRule
{
    public override string RuleName => nameof(KeyValuePairMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsKeyValuePair(compilation))
        {
            ruleArguments = null;
            return false;
        }

        var keySymbolType = namedTypeSymbol.TypeArguments[0];

        var keySerializedProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "key",
            keySymbolType,
            0,
            attributes,
            parentSymbol,
            null
        );

        var valueSymbolType = namedTypeSymbol.TypeArguments[1];

        var valueSerializedProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "value",
            valueSymbolType,
            1,
            attributes,
            parentSymbol,
            null
        );

        var keyArgumentsLength = keySerializedProperty.RuleArguments?.Length ?? 0;
        var valueArgumentsLength = valueSerializedProperty.RuleArguments?.Length ?? 0;

        ruleArguments = new string[6 + keyArgumentsLength + valueArgumentsLength];

        var index = 0;

        // Key
        ruleArguments[index++] = keySymbolType.ToDisplayString();
        ruleArguments[index++] = keySerializedProperty.Rule;
        ruleArguments[index++] = keyArgumentsLength.ToString();
        if (keyArgumentsLength > 0)
        {
            Array.Copy(keySerializedProperty.RuleArguments!, 0, ruleArguments, index, keyArgumentsLength);
            index += keyArgumentsLength;
        }

        // Value
        ruleArguments[index++] = valueSymbolType.ToDisplayString();
        ruleArguments[index++] = valueSerializedProperty.Rule;
        ruleArguments[index++] = valueArgumentsLength.ToString();

        if (valueArgumentsLength > 0)
        {
            Array.Copy(valueSerializedProperty.RuleArguments!, 0, ruleArguments, index, valueArgumentsLength);
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
        var index = property.RuleArguments![0] == "" ? 1 : 0; // Skip the blank argument option

        var keyType = ruleArguments![index++];
        var keyRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var keyRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (keyRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, keyRuleArguments, 0, keyRuleArguments.Length);
            index += keyRuleArguments.Length;
        }

        var valueType = ruleArguments[index++];
        var valueRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var valueRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (valueRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, valueRuleArguments, 0, valueRuleArguments.Length);
        }

        var propertyName = property.FieldName ?? property.Name;

        GenerateDeserialize(
            source,
            compilation,
            indent,
            propertyName,
            parentReference,
            keyType,
            valueType,
            keyRule,
            keyRuleArguments,
            valueRule,
            valueRuleArguments
        );
    }

    private static void GenerateDeserialize(
        StringBuilder source,
        Compilation compilation,
        string indent,
        string propertyName,
        string parentReference,
        string keyType,
        string valueType,
        ISerializableMigrationRule keyRule,
        string[] keyRuleArguments,
        ISerializableMigrationRule valueRule,
        string[] valueRuleArguments
    )
    {
        var serializableKeyProperty = new SerializableProperty
        {
            Name = "key",
            Type = keyType,
            Rule = keyRule.RuleName,
            RuleArguments = keyRuleArguments
        };

        keyRule.GenerateDeserializationMethod(
            source,
            indent,
            compilation,
            serializableKeyProperty,
            parentReference
        );

        var serializableValueProperty = new SerializableProperty
        {
            Name = "value",
            Type = valueType,
            Rule = valueRule.RuleName,
            RuleArguments = valueRuleArguments
        };

        valueRule.GenerateDeserializationMethod(
            source,
            indent,
            compilation,
            serializableValueProperty,
            parentReference
        );

        source.AppendLine(
            $"{indent}{propertyName} = new {SymbolMetadata.KEYVALUEPAIR_STRUCT}<{keyType}, {valueType}>(key, value);"
        );
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
        var index = property.RuleArguments![0] == "" ? 1 : 0; // Skip the blank argument option

        var keyType = ruleArguments![index++];
        var keyRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var keyRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (keyRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, keyRuleArguments, 0, keyRuleArguments.Length);
            index += keyRuleArguments.Length;
        }

        var valueType = ruleArguments[index++];
        var valueRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var valueRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (valueRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, valueRuleArguments, 0, valueRuleArguments.Length);
        }

        var propertyName = property.FieldName ?? property.Name;

        GenerateSerialize(
            source,
            indent,
            propertyName,
            keyType,
            valueType,
            keyRule,
            keyRuleArguments,
            valueRule,
            valueRuleArguments
        );
    }

    private static void GenerateSerialize(
        StringBuilder source,
        string indent,
        string propertyName,
        string keyType,
        string valueType,
        ISerializableMigrationRule keyRule,
        string[] keyRuleArguments,
        ISerializableMigrationRule valueRule,
        string[] valueRuleArguments
    )
    {
        var serializableKeyProperty = new SerializableProperty
        {
            Name = $"{propertyName}.Key",
            Type = keyType,
            Rule = keyRule.RuleName,
            RuleArguments = keyRuleArguments
        };

        keyRule.GenerateSerializationMethod(
            source,
            indent,
            serializableKeyProperty
        );

        var serializableValueProperty = new SerializableProperty
        {
            Name = $"{propertyName}.Value",
            Type = valueType,
            Rule = valueRule.RuleName,
            RuleArguments = valueRuleArguments
        };

        valueRule.GenerateSerializationMethod(
            source,
            indent,
            serializableValueProperty
        );
    }
}
