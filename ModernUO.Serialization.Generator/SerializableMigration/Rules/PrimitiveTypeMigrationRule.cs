/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PrimitiveTypeMigrationRule.cs                                   *
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

public class PrimitiveTypeMigrationRule : MigrationRule
{
    public override string RuleName => nameof(PrimitiveTypeMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol.IsIpAddress(compilation) || symbol.IsTimeSpan(compilation) || symbol.IsGuid(compilation) || symbol.IsType(compilation))
        {
            ruleArguments = Array.Empty<string>();
            return true;
        }

        if (
            symbol is not ITypeSymbol {
                SpecialType: not (not
                SpecialType.System_Boolean and not
                SpecialType.System_SByte and not
                SpecialType.System_Int16 and not
                SpecialType.System_Int32 and not
                SpecialType.System_Int64 and not
                SpecialType.System_Byte and not
                SpecialType.System_UInt16 and not
                SpecialType.System_UInt32 and not
                SpecialType.System_UInt64 and not
                SpecialType.System_Single and not
                SpecialType.System_Double and not
                SpecialType.System_String and not
                SpecialType.System_Decimal and not
                SpecialType.System_DateTime)
            } typeSymbol
        )
        {
            ruleArguments = null;
            return false;
        }

        ruleArguments = typeSymbol.SpecialType switch
        {
            SpecialType.System_Int32 when attributes.Any(a => a.IsEncodedInt(compilation)) =>
                new[] { "EncodedInt" },
            SpecialType.System_DateTime when attributes.Any(a => a.IsDeltaDateTime(compilation)) =>
                new[] { "DeltaTime" },
            SpecialType.System_String when attributes.Any(a => a.IsInternString(compilation)) =>
                new[] { "InternString" },
            _ => new[] { "" }
        };

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

        var propertyName = property.FieldName ?? property.Name;
        var argument = property.RuleArguments?.Length >= 1 ? property.RuleArguments[0] : null;

        const string date = SymbolMetadata.DATETIME_STRUCT;

        var readMethod = property.Type switch
        {
            "bool"                              => "ReadBool",
            "sbyte"                             => "ReadSByte",
            "short"                             => "ReadShort",
            "int" when argument == "EncodedInt" => "ReadEncodedInt",
            "int"                               => "ReadInt",
            "long"                              => "ReadLong",
            "byte"                              => "ReadByte",
            "ushort"                            => "ReadUShort",
            "uint"                              => "ReadUInt",
            "ulong"                             => "ReadULong",
            "float"                             => "ReadFloat",
            "double"                            => "ReadDouble",
            "string"                            => "ReadString",
            "decimal"                           => "ReadDecimal",
            date when argument == "DeltaTime"   => "ReadDeltaTime",
            date                                => "ReadDateTime",
            SymbolMetadata.IPADDRESS_CLASS      => "ReadIPAddress",
            SymbolMetadata.TIMESPAN_STRUCT      => "ReadTimeSpan",
            SymbolMetadata.GUID_STRUCT          => "ReadGuid",
            SymbolMetadata.TYPE_CLASS           => "ReadType"
        };

        var readArgument = readMethod == "ReadString" && argument == "InternString" ? "true" : "";

        source.AppendLine($"{indent}{propertyName} = reader.{readMethod}({readArgument});");
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
        var argument = property.RuleArguments?.Length >= 1 ? property.RuleArguments[0] : null;

        const string date = SymbolMetadata.DATETIME_STRUCT;
        const string type = SymbolMetadata.TYPE_CLASS;

        var writeMethod = property.Type switch
        {
            type                                => "WriteType",
            date when argument == "DeltaTime"   => "WriteDeltaTime",
            "int" when argument == "EncodedInt" => "WriteEncodedInt",
            _                                   => "Write"
        };

        source.AppendLine($"{indent}writer.{writeMethod}({propertyName});");
    }
}
