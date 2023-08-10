/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor SG3001 = new(
        "SG3001",
        "Classes marked with the SerializationGenerator attribute must be partial",
        "'{0}' must be a partial class to use the SerializationGenerator attribute.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3002 = new(
        "SG3002",
        "Classes marked with the SerializationGenerator attribute must properly import the attribute",
        "'{0}' is not properly importing the SerializationGenerator attribute.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3003 = new(
        "SG3003",
        "Duplicate attribute found",
        "Duplicate {0} attribute found for property '{1}'.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3004 = new(
        "SG3004",
        "SerializableProperty attribute argument 'useField' is invalid.",
        "The field '{0}' for SerializableProperty attribute '{1}' cannot be found.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3005 = new(
        "SG3005",
        "Order of serializable fields is invalid",
        "Expected field '{0}' with order `{1}` but found `{2}'.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3006 = new(
        "SG3006",
        "Serializable field order argument must be positive",
        "{0} for '{1}' must be positive.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3007 = new(
        "SG3007",
        "No migration rule found",
        "No migration rule found for field '{0}' of type '{1}'. If this is a bug, please notify the author.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3008 = new(
        "SG3008",
        "Missing DeserializeTimerField attribute",
        "Missing DeserializeTimerField attribute for '{0}'.",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor GeneratorCrashedDiagnostic(Exception e) =>
        new(
            "SG0001",
            "Source generator crashed due to an internal error.",
            @"Serialization Generator threw an exception of type '{0}' while generating {1} with message '{2}'",
            "ModernUO.Serialization.Generator",
            DiagnosticSeverity.Error,
            true,
            description: $"Serialization Generator threw the following exception: '{e.CreateDiagnosticDescription()}'",
            customTags: WellKnownDiagnosticTags.AnalyzerException
        );

    private static readonly string _separator = $"{Environment.NewLine}-----{Environment.NewLine}";

    public static string CreateDiagnosticDescription(this Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            var flattened = aggregateException.Flatten();
            return string.Join(_separator, flattened.InnerExceptions.Select(GetExceptionMessage));
        }

        if (exception != null)
        {
            return string.Join(
                _separator,
                GetExceptionMessage(exception),
                CreateDiagnosticDescription(exception.InnerException)
            );
        }

        return string.Empty;
    }

    private static string GetExceptionMessage(Exception exception)
    {
        var fusionLog = (exception as FileNotFoundException)?.FusionLog;
        return fusionLog == null ? exception.ToString() : string.Join(_separator, exception.Message, fusionLog);
    }
}
