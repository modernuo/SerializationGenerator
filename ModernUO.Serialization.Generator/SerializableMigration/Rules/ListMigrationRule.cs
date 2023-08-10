/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

        var isTidy = attributes.Any(a => a.IsTidy(compilation));
        var canBeNull = attributes.Any(a => a.IsCanBeNull(compilation));

        var length = serializableListType.RuleArguments?.Length ?? 0;
        ruleArguments = new string[(isTidy ? 1 : 0) + (canBeNull ? 1 : 0) + 2 + length];

        var index = 0;

        if (isTidy)
        {
            ruleArguments[index++] = "@Tidy";
        }

        if (canBeNull)
        {
            ruleArguments[index++] = "@CanBeNull";
        }
        ruleArguments[index++] = listTypeSymbol.ToDisplayString();
        ruleArguments[index++] = serializableListType.Rule;

        if (length > 0)
        {
            Array.Copy(serializableListType.RuleArguments!, 0, ruleArguments, index, length);
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

        var listElementType = ruleArguments![index++];
        var listElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var listElementRuleArguments = new string[ruleArguments.Length - index];
        Array.Copy(ruleArguments, index, listElementRuleArguments, 0, ruleArguments.Length - index);

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
                listElementType,
                listElementRule,
                listElementRuleArguments
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
                listElementType,
                listElementRule,
                listElementRuleArguments
            );
        }
    }

    private static void GenerateDeserialize(
        StringBuilder source,
        Compilation compilation,
        string indent,
        string propertyName,
        string parentReference,
        string listElementType,
        ISerializableMigrationRule listElementRule,
        string[] listElementRuleArguments
    )
    {
        var propertyIndex = $"{propertyName}Index";
        var propertyEntry = $"{propertyName}Entry";
        var propertyCount = $"{propertyName}Count";

        source.AppendLine($"{indent}{listElementType} {propertyEntry};");
        source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
        source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.List<{listElementType}>({propertyCount});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableListElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = listElementType,
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
        var shouldTidy = property.RuleArguments![0] == "@Tidy";
        var index = shouldTidy || property.RuleArguments![0] == "" ? 1 : 0; // Skip the empty argyment
        var canBeNull = property.RuleArguments[index] == "@CanBeNull";

        if (canBeNull)
        {
            index++;
        }

        var listElementType = ruleArguments![index++];
        var listElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var listElementRuleArguments = new string[ruleArguments.Length - index];
        Array.Copy(ruleArguments, index, listElementRuleArguments, 0, ruleArguments.Length - index);

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
                listElementType,
                listElementRule,
                listElementRuleArguments
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
                listElementType,
                listElementRule,
                listElementRuleArguments
            );
        }
    }

    private static void GenerateSerialize(
        StringBuilder source,
        string indent,
        string propertyName,
        bool shouldTidy,
        string listElementType,
        ISerializableMigrationRule listElementRule,
        string[] listElementRuleArguments
    )
    {
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
            Type = listElementType,
            Rule = listElementRule.RuleName,
            RuleArguments = listElementRuleArguments
        };

        listElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableListElement);

        source.AppendLine($"{indent}    }}");
        source.AppendLine($"{indent}}}");
    }
}
