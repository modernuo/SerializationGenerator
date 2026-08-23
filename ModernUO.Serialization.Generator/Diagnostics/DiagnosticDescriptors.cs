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
        "Types marked with the SerializationGenerator attribute must be partial",
        "'{0}' must be a partial type to use the SerializationGenerator attribute",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3002 = new(
        "SG3002",
        "Types marked with the SerializationGenerator attribute must properly import the attribute",
        "'{0}' is not properly importing the SerializationGenerator attribute",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3003 = new(
        "SG3003",
        "Duplicate attribute found",
        "Duplicate {0} attribute found for property '{1}'",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3004 = new(
        "SG3004",
        "SerializableProperty attribute argument 'useField' is invalid",
        "The field '{0}' for SerializableProperty attribute '{1}' cannot be found",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3005 = new(
        "SG3005",
        "Order of serializable fields is invalid",
        "Expected field '{0}' with order `{1}` but found `{2}'",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3006 = new(
        "SG3006",
        "Serializable field order argument must be positive",
        "{0} for '{1}' must be positive",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3007 = new(
        "SG3007",
        "No migration rule found",
        "No migration rule found for field '{0}' of type '{1}'",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3008 = new(
        "SG3008",
        "Missing DeserializeTimer attribute",
        "Serializable timer field '{0}' must declare [DeserializeTimer(nameof(Method))]",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3009 = new(
        "SG3009",
        "Struct/record must not declare a Deserialize method",
        "'{0}' must not declare a 'Deserialize(IGenericReader)' method or factory; the generator provides 'void Deserialize(IGenericReader)' for value types",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    // SG3010 (changed-method signature), SG3014 (unknown field reference), SG3016
    // (conflicting linkage styles), and SG3017 (default without save flag) were retired in
    // v4: field-side linkage makes those mistakes unrepresentable, and SG3015 covers every
    // remaining method-resolution failure. Do not reuse the numbers.

    public static readonly DiagnosticDescriptor SG3011 = new(
        "SG3011",
        "Duplicate migration file ignored",
        "Duplicate migration file '{0}' was ignored; another file already defines this class and version",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor SG3012 = new(
        "SG3012",
        "Stale migration file",
        "Migration file for version {0} is above the current version {1} and is never read",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor SG3013 = new(
        "SG3013",
        "Invalid migration file",
        "Migration file '{0}' could not be parsed: {1}",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3015 = new(
        "SG3015",
        "Linked method not found or invalid",
        "Method '{0}' referenced by [{1}] was not found or does not match the expected signature '{2}'",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SG3018 = new(
        "SG3018",
        "fieldChanged requires a generated setter",
        "The fieldChanged callback for '{0}' can never fire because no setter is generated (readonly field or omitted setter)",
        "ModernUO.Serialization.Generator",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor GeneratorCrashedDiagnostic(Exception e) =>
        new(
            "SG0001",
            "Source generator crashed due to an internal error",
            "Serialization Generator threw an exception of type '{0}' while generating {1} with message '{2}'",
            "ModernUO.Serialization.Generator",
            DiagnosticSeverity.Error,
            true,
            description: $"Serialization Generator threw the following exception: '{e.CreateDiagnosticDescription()}'",
            customTags: WellKnownDiagnosticTags.AnalyzerException
        );

    private static readonly string _separator = $"\n-----\n";

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
