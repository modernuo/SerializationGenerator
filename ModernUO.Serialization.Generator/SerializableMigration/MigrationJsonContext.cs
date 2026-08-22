/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MigrationJsonContext.cs                                         *
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

/// <summary>
/// Source-generated serializer metadata for the migration schema types, so parsing thousands
/// of migration files never pays reflection-based metadata construction inside the analyzer
/// process.
/// </summary>
[JsonSerializable(typeof(SerializableMetadata))]
[JsonSerializable(typeof(SerializableProperty))]
internal partial class MigrationJsonContext : JsonSerializerContext
{
}
