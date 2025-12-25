/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SortedSetComparerTests.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class SortedSetComparerTests
{
    [Fact]
    public void SortedSet_WithInstanceComparer_GeneratesCorrectConstructor()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                public class CaseInsensitiveComparer : IComparer<string>
                {
                    public int Compare(string x, string y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
                }

                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    [SortedSetComparer(typeof(CaseInsensitiveComparer))]
                    private SortedSet<string> _names;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Should generate constructor with new comparer instance
        Assert.Contains("new TestNamespace.CaseInsensitiveComparer()", generatedSource);
    }

    [Fact]
    public void SortedSet_WithStaticMemberComparer_GeneratesCorrectConstructor()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    [SortedSetComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
                    private SortedSet<string> _names;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Should generate constructor with static member reference
        Assert.Contains("System.StringComparer.OrdinalIgnoreCase", generatedSource);
    }

    [Fact]
    public void SortedSet_WithoutComparer_GeneratesDefaultConstructor()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    private SortedSet<int> _numbers;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Should generate constructor without comparer argument
        Assert.Contains("new System.Collections.Generic.SortedSet<int>();", generatedSource);
    }

    [Fact]
    public void SortedSet_StringWithoutComparer_GeneratesDefaultConstructor()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    private SortedSet<string> _names;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Should generate constructor without comparer argument
        Assert.Contains("new System.Collections.Generic.SortedSet<string>();", generatedSource);
    }

    [Fact]
    public void SortedSet_WithComparerAndCanBeNull_GeneratesCorrectCode()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    [CanBeNull]
                    [SortedSetComparer(typeof(StringComparer), nameof(StringComparer.Ordinal))]
                    private SortedSet<string> _names;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Should include both the null check and the comparer
        Assert.Contains("reader.ReadBool()", generatedSource);
        Assert.Contains("System.StringComparer.Ordinal", generatedSource);
    }

    [Fact]
    public void SortedSet_WithComparerAndTidy_GeneratesCorrectCode()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    [Tidy]
                    [SortedSetComparer(typeof(StringComparer), nameof(StringComparer.CurrentCultureIgnoreCase))]
                    private SortedSet<string> _names;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Should include Tidy call and comparer
        Assert.Contains(".Tidy()", generatedSource);
        Assert.Contains("System.StringComparer.CurrentCultureIgnoreCase", generatedSource);
    }
}
