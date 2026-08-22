/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableProperty.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text.Json.Serialization;

namespace ModernUO.Serialization.Generator;

public record SerializableProperty
{
    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; }

    [JsonPropertyName("usesSaveFlag")]
    public bool? UsesSaveFlag { get; init; }

    [JsonPropertyName("rule")]
    public string Rule { get; init; }

    [JsonPropertyName("ruleArguments")]
    public string[]? RuleArguments { get; init; }

    [JsonIgnore]
    public int Order { get; init; }

    [JsonIgnore]
    public string? FieldName { get; init; }

    [JsonIgnore]
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Whether <see cref="Type" /> resolves to a value type in the consuming compilation.
    /// Filled by the pipeline for migration properties; not part of the schema.
    /// </summary>
    [JsonIgnore]
    public bool? TypeIsValueType { get; init; }

    // string[] compares by reference under default record equality; the pipeline caches on
    // value equality, so compare the sequence.
    public virtual bool Equals(SerializableProperty? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Name != other.Name || Type != other.Type || UsesSaveFlag != other.UsesSaveFlag ||
            Rule != other.Rule || Order != other.Order || FieldName != other.FieldName ||
            IsReadOnly != other.IsReadOnly || TypeIsValueType != other.TypeIsValueType)
        {
            return false;
        }

        if (RuleArguments is null)
        {
            return other.RuleArguments is null;
        }

        if (other.RuleArguments is null || RuleArguments.Length != other.RuleArguments.Length)
        {
            return false;
        }

        for (var i = 0; i < RuleArguments.Length; i++)
        {
            if (RuleArguments[i] != other.RuleArguments[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            hash = hash * 31 + (Type?.GetHashCode() ?? 0);
            hash = hash * 31 + (Rule?.GetHashCode() ?? 0);
            hash = hash * 31 + Order;

            if (RuleArguments != null)
            {
                for (var i = 0; i < RuleArguments.Length; i++)
                {
                    hash = hash * 31 + (RuleArguments[i]?.GetHashCode() ?? 0);
                }
            }

            return hash;
        }
    }
}
