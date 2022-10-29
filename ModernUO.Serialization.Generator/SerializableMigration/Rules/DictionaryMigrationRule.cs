/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DictionaryMigrationRule.cs                                      *
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

public class DictionaryMigrationRule : MigrationRule
{
    public override string RuleName => nameof(DictionaryMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsDictionary(compilation))
        {
            ruleArguments = null;
            return false;
        }

        var keySymbolType = namedTypeSymbol.TypeArguments[0];

        var serializableKeyProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "KeyEntry",
            keySymbolType,
            0,
            attributes,
            parentSymbol,
            null
        );

        var valueSymbolType = namedTypeSymbol.TypeArguments[1];

        var serializableValueProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "ValueEntry",
            valueSymbolType,
            0,
            attributes,
            parentSymbol,
            null
        );

        var keyArgumentsLength = serializableKeyProperty.RuleArguments?.Length ?? 0;
        var valueArgumentsLength = serializableValueProperty.RuleArguments?.Length ?? 0;
        var isTidy = attributes.Any(a => a.IsTidy(compilation));
        var canBeNull = attributes.Any(a => a.IsCanBeNull(compilation));

        ruleArguments = new string[(isTidy ? 1 : 0) + (canBeNull ? 1 : 0) + 6 + keyArgumentsLength + valueArgumentsLength];

        var index = 0;

        if (isTidy)
        {
            ruleArguments[index++] = "@Tidy";
        }

        if (canBeNull)
        {
            ruleArguments[index++] = "@CanBeNull";
        }

        ruleArguments[index++] = keySymbolType.ToDisplayString();
        ruleArguments[index++] = serializableKeyProperty.Rule;
        ruleArguments[index++] = keyArgumentsLength.ToString();

        if (keyArgumentsLength > 0)
        {
            Array.Copy(serializableKeyProperty.RuleArguments!, 0, ruleArguments, index, keyArgumentsLength);
            index += keyArgumentsLength;
        }

        ruleArguments[index++] = valueSymbolType.ToDisplayString();
        ruleArguments[index++] = serializableValueProperty.Rule;
        ruleArguments[index++] = valueArgumentsLength.ToString();

        if (valueArgumentsLength > 0)
        {
            Array.Copy(serializableValueProperty.RuleArguments!, 0, ruleArguments, index, valueArgumentsLength);
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
        var index = property.RuleArguments![0] is "@Tidy" or "" ? 1 : 0; // Skip the blank argument option
        var canBeNull = property.RuleArguments[index] == "@CanBeNull";

        if (canBeNull)
        {
            index++;
        }

        var keyType = ruleArguments![index++];

        var keyElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var keyRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (keyRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, keyRuleArguments, 0, keyRuleArguments.Length);
            index += keyRuleArguments.Length;
        }

        var valueType = ruleArguments[index++];
        var valueElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var valueRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (valueRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, valueRuleArguments, 0, valueRuleArguments.Length);
        }

        var propertyName = property.FieldName ?? property.Name;

        if (canBeNull)
        {
            source.AppendLine($"{indent}if (reader.ReadBool())");
            source.AppendLine($"{indent}{{");
            GenerateDeserialize(
                source,
                compilation,
                $"{indent}    ",
                propertyName,
                parentReference,
                keyType,
                valueType,
                keyElementRule,
                keyRuleArguments,
                valueElementRule,
                valueRuleArguments
            );
            source.AppendLine($"{indent}}}");
            source.AppendLine($"{indent}else");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName} = default;");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            GenerateDeserialize(
                source,
                compilation,
                indent,
                propertyName,
                parentReference,
                keyType,
                valueType,
                keyElementRule,
                keyRuleArguments,
                valueElementRule,
                valueRuleArguments
            );
        }
    }

    private static void GenerateDeserialize(
        StringBuilder source,
        Compilation compilation,
        string indent,
        string propertyName,
        string parentReference,
        string keyType,
        string valueType,
        ISerializableMigrationRule keyElementRule,
        string[] keyRuleArguments,
        ISerializableMigrationRule valueElementRule,
        string[] valueRuleArguments
    )
    {
        var propertyIndex = $"{propertyName}Index";
        var propertyKeyEntry = $"{propertyName}Key";
        var propertyValueEntry = $"{propertyName}Value";
        var propertyCount = $"{propertyName}Count";

        source.AppendLine($"{indent}{keyType} {propertyKeyEntry};");
        source.AppendLine($"{indent}{valueType} {propertyValueEntry};");
        source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
        source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.Dictionary<{keyType}, {valueType}>({propertyCount});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableKeyElement = new SerializableProperty
        {
            Name = propertyKeyEntry,
            Type = keyType,
            Rule = keyElementRule.RuleName,
            RuleArguments = keyRuleArguments
        };

        keyElementRule.GenerateDeserializationMethod(
            source,
            $"{indent}    ",
            compilation,
            serializableKeyElement,
            parentReference
        );

        var serializableValueElement = new SerializableProperty
        {
            Name = propertyValueEntry,
            Type = valueType,
            Rule = valueElementRule.RuleName,
            RuleArguments = valueRuleArguments
        };

        valueElementRule.GenerateDeserializationMethod(
            source,
            $"{indent}    ",
            compilation,
            serializableValueElement,
            parentReference
        );
        source.AppendLine($"{indent}    {propertyName}.Add({propertyKeyEntry}, {propertyValueEntry});");

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
        var shouldTidy = property.RuleArguments![0] == "@Tidy";
        var index = shouldTidy || property.RuleArguments![0] == "" ? 1 : 0; // Skip the empty argyment
        var canBeNull = property.RuleArguments[index] == "@CanBeNull";

        if (canBeNull)
        {
            index++;
        }

        var keyType = ruleArguments![index++];
        var keyElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments![index++]];
        var keyRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (keyRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, keyRuleArguments, 0, keyRuleArguments.Length);
            index += keyRuleArguments.Length;
        }

        var valueType = ruleArguments[index++];
        var valueElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var valueRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (valueRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, valueRuleArguments, 0, valueRuleArguments.Length);
        }

        var propertyName = property.FieldName ?? property.Name;

        if (canBeNull)
        {
            var newIndent = $"{indent}    ";
            source.AppendLine($"{indent}if ({propertyName} != default)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{newIndent}writer.Write(true);");
            GenerateSerialize(
                source,
                newIndent,
                propertyName,
                shouldTidy,
                keyType,
                valueType,
                keyElementRule,
                keyRuleArguments,
                valueElementRule,
                valueRuleArguments
            );
            source.AppendLine($"{indent}}}");
            source.AppendLine($"{indent}else");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{newIndent}writer.Write(false);");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            GenerateSerialize(
                source,
                indent,
                propertyName,
                shouldTidy,
                keyType,
                valueType,
                keyElementRule,
                keyRuleArguments,
                valueElementRule,
                valueRuleArguments
            );
        }
    }

    private static void GenerateSerialize(
        StringBuilder source,
        string indent,
        string propertyName,
        bool shouldTidy,
        string keyType,
        string valueType,
        ISerializableMigrationRule keyElementRule,
        string[] keyRuleArguments,
        ISerializableMigrationRule valueElementRule,
        string[] valueRuleArguments
    )
    {
        var propertyKeyEntry = $"{propertyName}Key";
        var propertyValueEntry = $"{propertyName}Value";
        var propertyCount = $"{propertyName}Count";

        if (shouldTidy)
        {
            source.AppendLine($"{indent}{propertyName}?.Tidy();");
        }
        source.AppendLine($"{indent}var {propertyCount} = {propertyName}?.Count ?? 0;");
        source.AppendLine($"{indent}writer.WriteEncodedInt({propertyCount});");
        source.AppendLine($"{indent}if ({propertyCount} > 0)");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    foreach (var ({propertyKeyEntry}, {propertyValueEntry}) in {propertyName}!)");
        source.AppendLine($"{indent}    {{");

        var newIndent = $"{indent}        ";
        var serializableKeyElement = new SerializableProperty
        {
            Name = propertyKeyEntry,
            Type = keyType,
            Rule = keyElementRule.RuleName,
            RuleArguments = keyRuleArguments
        };

        keyElementRule.GenerateSerializationMethod(source, newIndent, serializableKeyElement);

        var serializableValueElement = new SerializableProperty
        {
            Name = propertyValueEntry,
            Type = valueType,
            Rule = valueElementRule.RuleName,
            RuleArguments = valueRuleArguments
        };

        valueElementRule.GenerateSerializationMethod(source, newIndent, serializableValueElement);

        source.AppendLine($"{indent}    }}");
        source.AppendLine($"{indent}}}");
    }
}
