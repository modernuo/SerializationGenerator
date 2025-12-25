/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenericSerializationTests.cs                                    *
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

public class GenericSerializationTests
{
    [Fact]
    public void GenericClass_SingleTypeParameter_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class GenericItem<T> : ISerializable where T : struct
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial class GenericItem<T>", generatedSource);
        Assert.Contains("where T : struct", generatedSource);
    }

    [Fact]
    public void GenericClass_MultipleTypeParameters_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class GenericItem<TKey, TValue> : ISerializable
                    where TKey : class
                    where TValue : struct
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial class GenericItem<TKey, TValue>", generatedSource);
        Assert.Contains("where TKey : class", generatedSource);
        Assert.Contains("where TValue : struct", generatedSource);
    }

    [Fact]
    public void GenericClass_ConstraintWithNew_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class GenericItem<T> : ISerializable where T : class, new()
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("where T : class, new()", generatedSource);
    }

    [Fact]
    public void GenericClass_NoConstraints_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class GenericItem<T> : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial class GenericItem<T>", generatedSource);
        // Should not have where clause
        Assert.DoesNotContain("where T :", generatedSource);
    }

    [Fact]
    public void GenericClass_NestedInGenericClass_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                public partial class OuterGeneric<TOuter>
                {
                    [SerializationGenerator(0)]
                    public partial class InnerItem : ISerializable
                    {
                        [SerializableField(0)]
                        private string _name;

                        public Serial Serial => default;
                        public void MarkDirty() { }
                    }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial class OuterGeneric<TOuter>", generatedSource);
        Assert.Contains("partial class InnerItem", generatedSource);
    }

    [Fact]
    public void GenericClass_InterfaceConstraint_GeneratesCorrectly()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class GenericItem<T> : ISerializable where T : IComparable<T>
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("where T : System.IComparable<T>", generatedSource);
    }
}
