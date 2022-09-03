/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArrayMigrationRule.cs                                           *
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

public class ArrayMigrationRule : MigrationRule
{
    public override string RuleName => nameof(ArrayMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not IArrayTypeSymbol arrayTypeSymbol)
        {
            ruleArguments = null;
            return false;
        }

        var serializableArrayType = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "ArrayEntry",
            arrayTypeSymbol.ElementType,
            0,
            attributes,
            parentSymbol,
            null
        );

        var length = serializableArrayType.RuleArguments?.Length?? 0;
        ruleArguments = new string[length + 2];
        ruleArguments[0] = arrayTypeSymbol.ElementType.ToDisplayString();
        ruleArguments[1] = serializableArrayType.Rule;
        if (length > 0)
        {
            Array.Copy(serializableArrayType.RuleArguments!, 0, ruleArguments, 2, length);
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
        var arrayElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments![1]];
        var arrayElementRuleArguments = new string[ruleArguments.Length - 2];
        Array.Copy(ruleArguments, 2, arrayElementRuleArguments, 0, ruleArguments.Length - 2);

        var propertyName = property.FieldName ?? property.Name;
        var propertyIndex = $"{property.Name}Index";
        source.AppendLine($"{indent}{propertyName} = new {ruleArguments[0]}[reader.ReadEncodedInt()];");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyName}.Length; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableArrayElement = new SerializableProperty
        {
            Name = $"{propertyName}[{propertyIndex}]",
            Type = ruleArguments[0],
            Rule = arrayElementRule.RuleName,
            RuleArguments = arrayElementRuleArguments
        };

        arrayElementRule.GenerateDeserializationMethod(
            source,
            $"{indent}    ",
            compilation,
            serializableArrayElement,
            parentReference
        );

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
        var arrayElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments![1]];
        var arrayElementRuleArguments = new string[ruleArguments.Length - 2];
        Array.Copy(ruleArguments, 2, arrayElementRuleArguments, 0, ruleArguments.Length - 2);

        var propertyName = property.FieldName ?? property.Name;
        var propertyIndex = $"{propertyName}Index";
        var propertyLength = $"{propertyName}Length";
        source.AppendLine($"{indent}var {propertyLength} = {propertyName}?.Length ?? 0;");
        source.AppendLine($"{indent}writer.WriteEncodedInt({propertyLength});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyLength}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableArrayElement = new SerializableProperty
        {
            Name = $"{propertyName}![{propertyIndex}]",
            Type = ruleArguments[0],
            Rule = arrayElementRule.RuleName,
            RuleArguments = arrayElementRuleArguments
        };

        arrayElementRule.GenerateSerializationMethod(source, $"{indent}    ", serializableArrayElement);

        source.AppendLine($"{indent}}}");
    }
}
