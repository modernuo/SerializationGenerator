/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HashSetMigrationRule.cs                                         *
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

public class HashSetMigrationRule : MigrationRule
{
    public override string RuleName => nameof(HashSetMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsHashSet(compilation))
        {
            ruleArguments = null;
            return false;
        }

        var setTypeSymbol = namedTypeSymbol.TypeArguments[0];

        var serializableSetType = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "SetEntry",
            setTypeSymbol,
            0,
            attributes,
            parentSymbol,
            null
        );

        var isTidy = attributes.Any(a => a.IsTidy(compilation));
        var canBeNull = attributes.Any(a => a.IsCanBeNull(compilation));

        var length = serializableSetType.RuleArguments?.Length ?? 0;
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
        ruleArguments[index++] = setTypeSymbol.ToDisplayString();
        ruleArguments[index++] = serializableSetType.Rule;

        if (length > 0)
        {
            Array.Copy(serializableSetType.RuleArguments!, 0, ruleArguments, index, length);
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

        var setElementType = ruleArguments![index++];
        var setElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var setElementRuleArguments = new string[ruleArguments.Length - index];

        Array.Copy(ruleArguments, index, setElementRuleArguments, 0, ruleArguments.Length - index);

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
                setElementType,
                setElementRule,
                setElementRuleArguments
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
                setElementType,
                setElementRule,
                setElementRuleArguments
            );
        }
    }

    private static void GenerateDeserialize(
        StringBuilder source,
        Compilation compilation,
        string indent,
        string propertyName,
        string parentReference,
        string setElementType,
        ISerializableMigrationRule setElementRule,
        string[] setElementRuleArguments
    )
    {
        var propertyIndex = $"{propertyName}Index";
        var propertyEntry = $"{propertyName}Entry";
        var propertyCount = $"{propertyName}Count";

        source.AppendLine($"{indent}{setElementType} {propertyEntry};");
        source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
        source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.HashSet<{setElementType}>({propertyCount});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableSetElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = setElementType,
            Rule = setElementRule.RuleName,
            RuleArguments = setElementRuleArguments
        };

        setElementRule.GenerateDeserializationMethod(
            source,
            $"{indent}    ",
            compilation,
            serializableSetElement,
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

        var setElementType = ruleArguments![index++];
        var setElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var setElementRuleArguments = new string[ruleArguments.Length - index];
        Array.Copy(ruleArguments, index, setElementRuleArguments, 0, ruleArguments.Length - index);

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
                setElementType,
                setElementRule,
                setElementRuleArguments
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
                setElementType,
                setElementRule,
                setElementRuleArguments
            );
        }
    }

    private static void GenerateSerialize(
        StringBuilder source,
        string indent,
        string propertyName,
        bool shouldTidy,
        string setElementType,
        ISerializableMigrationRule setElementRule,
        string[] setElementRuleArguments
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

        var serializableSetElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = setElementType,
            Rule = setElementRule.RuleName,
            RuleArguments = setElementRuleArguments
        };

        setElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableSetElement);

        source.AppendLine($"{indent}    }}");
        source.AppendLine($"{indent}}}");
    }
}
