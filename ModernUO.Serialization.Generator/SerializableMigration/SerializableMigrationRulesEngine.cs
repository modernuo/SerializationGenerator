/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigrationRulesEngine.cs                             *
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
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static class SerializableMigrationRulesEngine
{
    public static readonly Dictionary<string, ISerializableMigrationRule> Rules = new();

    static SerializableMigrationRulesEngine()
    {
        var rules = new ISerializableMigrationRule[]
        {
            new EnumMigrationRule(),
            new ListMigrationRule(),
            new ArrayMigrationRule(),
            new HashSetMigrationRule(),
            new DictionaryMigrationRule(),
            new KeyValuePairMigrationRule(),
            new PrimitiveTypeMigrationRule(),
            new PrimitiveUOTypeMigrationRule(),
            new SerializableInterfaceMigrationRule(),
            new SerializationMethodSignatureMigrationRule(),
            new TimerMigrationRule(),
            new RawSerializableMigrationRule()
        };

        foreach (var rule in rules)
        {
            Rules.Add(rule.RuleName, rule);
        }
    }

    public static SerializableProperty GenerateSerializableProperty(
        Compilation compilation,
        string propertyName,
        ISymbol propertyType,
        int order,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        SerializableFieldSaveFlagMethods? serializableFieldSaveFlagMethods
    )
    {
        foreach (var rule in Rules.Values)
        {
            if (rule.GenerateRuleState(
                    compilation,
                    propertyType,
                    attributes,
                    parentSymbol,
                    out var ruleArguments
                ))
            {
                return new SerializableProperty
                {
                    Name = propertyName,
                    Type = propertyType.ToDisplayString(),
                    Order = order,
                    UsesSaveFlag = serializableFieldSaveFlagMethods?.DetermineFieldShouldSerialize != null ? true : null,
                    Rule = rule.RuleName,
                    RuleArguments = ruleArguments.Length > 0 ? ruleArguments : null
                };
            }
        }

        throw new NoRuleFoundException(propertyName, propertyType.ToDisplayString());
    }
}
