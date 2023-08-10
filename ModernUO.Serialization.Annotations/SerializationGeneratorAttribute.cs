/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationGeneratorAttribute.cs                              *
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

namespace ModernUO.Serialization;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SerializationGeneratorAttribute : Attribute
{
    public int Version { get; }
    public bool EncodedVersion { get; }

    public SerializationGeneratorAttribute(int version, bool encodedVersion = true)
    {
        Version = version;
        EncodedVersion = encodedVersion;
    }
}
