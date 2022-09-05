/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DiagnosticDescriptors.cs                                        *
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
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor SG3001 = new(
        "SG3001",
        "Classes marked with the SerializationGenerator attribute must be partial.",
        "{0} must be a partial class to use the SerializationGenerator attribute.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3002 = new(
        "SG3002",
        "Classes marked with the SerializationGenerator attribute must properly import the attribute.",
        "{0} is not properly importing the SerializationGenerator attribute.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG9999 = new(
        "SG9999",
        "Source generator crashed due to an internal error.",
        "{0} could not be generated due to an internal crash. Error: {1}. {2}",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

}
