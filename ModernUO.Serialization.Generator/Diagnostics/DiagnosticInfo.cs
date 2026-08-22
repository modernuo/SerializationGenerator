/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DiagnosticInfo.cs                                               *
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ModernUO.Serialization.Generator;

/// <summary>
/// A value-equatable stand-in for <see cref="Diagnostic" /> so pipeline models stay cacheable;
/// materialized back into a real diagnostic at output time.
/// </summary>
public sealed record DiagnosticInfo(
    string Id,
    string FilePath,
    TextSpan Span,
    LinePositionSpan LineSpan,
    EquatableArray<string> Args
)
{
    private static readonly Dictionary<string, DiagnosticDescriptor> _descriptors = new()
    {
        ["SG3001"] = DiagnosticDescriptors.SG3001,
        ["SG3002"] = DiagnosticDescriptors.SG3002,
        ["SG3003"] = DiagnosticDescriptors.SG3003,
        ["SG3004"] = DiagnosticDescriptors.SG3004,
        ["SG3005"] = DiagnosticDescriptors.SG3005,
        ["SG3006"] = DiagnosticDescriptors.SG3006,
        ["SG3007"] = DiagnosticDescriptors.SG3007,
        ["SG3008"] = DiagnosticDescriptors.SG3008,
        ["SG3009"] = DiagnosticDescriptors.SG3009,
        ["SG3010"] = DiagnosticDescriptors.SG3010,
        ["SG3011"] = DiagnosticDescriptors.SG3011,
        ["SG3012"] = DiagnosticDescriptors.SG3012,
        ["SG3013"] = DiagnosticDescriptors.SG3013,
        ["SG3014"] = DiagnosticDescriptors.SG3014,
        ["SG3015"] = DiagnosticDescriptors.SG3015,
        ["SG3016"] = DiagnosticDescriptors.SG3016,
        ["SG3017"] = DiagnosticDescriptors.SG3017
    };

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params object[] args)
    {
        var lineSpan = location.GetLineSpan();
        return new DiagnosticInfo(
            descriptor.Id,
            location.SourceTree?.FilePath ?? lineSpan.Path ?? "",
            location.SourceSpan,
            lineSpan.Span,
            args.Select(a => a?.ToString() ?? "").ToEquatableArray()
        );
    }

    public Diagnostic ToDiagnostic() =>
        Diagnostic.Create(
            _descriptors[Id],
            Location.Create(FilePath, Span, LineSpan),
            Args.Cast<object>().ToArray()
        );
}
