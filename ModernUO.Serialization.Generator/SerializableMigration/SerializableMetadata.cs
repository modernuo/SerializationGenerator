/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigration.cs                                        *
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
using System.Text.Json.Serialization;

namespace ModernUO.Serialization.Generator;

public record SerializableMetadata
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; }

    [JsonPropertyName("properties")]
    public ImmutableArray<SerializableProperty>? Properties { get; init; }

    // ImmutableArray compares by reference under default record equality; the pipeline caches
    // on value equality, so compare the sequence.
    public virtual bool Equals(SerializableMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Version != other.Version || Type != other.Type)
        {
            return false;
        }

        var a = Properties;
        var b = other.Properties;

        if (a is null || b is null)
        {
            return a is null == b is null;
        }

        if (a.Value.Length != b.Value.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Value.Length; i++)
        {
            if (!a.Value[i].Equals(b.Value[i]))
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
            hash = hash * 31 + Version;
            hash = hash * 31 + (Type?.GetHashCode() ?? 0);

            if (Properties is { } properties)
            {
                for (var i = 0; i < properties.Length; i++)
                {
                    hash = hash * 31 + properties[i].GetHashCode();
                }
            }

            return hash;
        }
    }
}
