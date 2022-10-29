/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableInterfaceMigrationRule.cs                           *
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

public class SerializableInterfaceMigrationRule : MigrationRule
{
    public override string RuleName => nameof(SerializableInterfaceMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is ITypeSymbol typeSymbol && typeSymbol.HasSerializableInterface(compilation))
        {
            var canBeNull = attributes.Any(a => a.IsCanBeNull(compilation));
            ruleArguments = canBeNull ? new[] { "@CanBeNull" } : Array.Empty<string>();
            return true;
        }

        ruleArguments = null;
        return false;
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
        var canBeNull = property.RuleArguments?.Length > 0 && property.RuleArguments[0] == "@CanBeNull";

        if (canBeNull)
        {
            source.AppendLine($"{indent}if (reader.ReadBool())");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    {propertyName} = reader.ReadEntity<{propertyType}>();");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            source.AppendLine($"{indent}{propertyName} = reader.ReadEntity<{propertyType}>();");
        }
    }

    public override void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var propertyType = property.Type;
        var propertyName = property.FieldName ?? property.Name;
        var canBeNull = property.RuleArguments?.Length > 0 && property.RuleArguments[0] == "@CanBeNull";

        if (canBeNull)
        {
            var newIndent = $"{indent}    ";
            source.AppendLine($"{indent}if ({propertyName} != default)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{newIndent}writer.Write(true);");
            source.AppendLine($"{newIndent}writer.Write({propertyName});");
            source.AppendLine($"{indent}}}");
            source.AppendLine($"{indent}else");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{newIndent}writer.Write(false);");
            source.AppendLine($"{indent}}}");
        }
        else
        {
            source.AppendLine($"{indent}{propertyName}.Serialize(writer);");
        }
    }
}
