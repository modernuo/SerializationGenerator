/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MigrationRule.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public abstract class MigrationRule : ISerializableMigrationRule
{
    public abstract string RuleName { get; }

    public virtual void GenerateMigrationProperty(
        StringBuilder source, Compilation compilation, string indent, SerializableProperty serializableProperty
    )
    {
        var propertyType = serializableProperty.Type;
        var type = compilation.GetTypeByMetadataName(propertyType)?.IsValueType == true
                   || SymbolMetadata.IsPrimitiveFromTypeDisplayString(propertyType) && propertyType != "bool"
            ? $"{propertyType}{(serializableProperty.UsesSaveFlag == true ? "?" : "")}" : propertyType;

        source.AppendLine($"{indent}internal readonly {type} {serializableProperty.Name};");
    }

    public abstract bool GenerateRuleState(
        Compilation compilation, ISymbol symbol, ImmutableArray<AttributeData> attributes,
        ISymbol? parentSymbol, out string[] ruleArguments
    );

    public abstract void GenerateDeserializationMethod(
        StringBuilder source,
        string indent,
        Compilation compilation,
        SerializableProperty property,
        string? parentReference,
        bool isMigration = false
    );

    public abstract void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property);
}
