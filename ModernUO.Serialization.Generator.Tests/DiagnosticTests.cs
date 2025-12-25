/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DiagnosticTests.cs                                              *
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

public class DiagnosticTests
{
    [Fact]
    public void SG3001_NonPartialClass_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public class NonPartialItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void Serialize(IGenericWriter writer) { }
                    public void Deserialize(IGenericReader reader) { }
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3001");
    }

    [Fact]
    public void SG3003_DuplicateFieldOrder_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class DuplicateOrderItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(0)]
                    private int _count;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3003");
    }

    [Fact]
    public void SG3004_InvalidUseField_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class InvalidFieldItem : ISerializable
                {
                    [SerializableProperty(0, useField: "_nonExistentField")]
                    public string Name { get; set; }

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3004");
    }

    [Fact]
    public void SG3005_NonSequentialOrder_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class GapOrderItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(2)]
                    private int _count;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3005");
    }

    [Fact]
    public void SG3006_NegativeOrder_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class NegativeOrderItem : ISerializable
                {
                    [SerializableField(-1)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3006");
    }

    [Fact]
    public void SG3007_UnsupportedType_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                public class UnsupportedType { }

                [SerializationGenerator(0)]
                public partial class UnsupportedItem : ISerializable
                {
                    [SerializableField(0)]
                    private UnsupportedType _unsupported;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3007");
    }

    [Fact]
    public void SG3009_StructWithoutDeserialize_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial struct MissingDeserializeStruct
                {
                    [SerializableField(0)]
                    private int _value;
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3009");
    }

    [Fact]
    public void ValidClass_NoDiagnostics()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ValidItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(1)]
                    private int _count;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
    }

    [Fact]
    public void DuplicateSaveFlagOrder_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class DuplicateFlagItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(1)]
                    private int _count;

                    [SerializableFieldSaveFlag(0)]
                    private bool ShouldSerializeName() => _name != null;

                    [SerializableFieldSaveFlag(0)]
                    private bool ShouldSerializeName2() => _name != null;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3003");
    }

    [Fact]
    public void NegativeSaveFlagOrder_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class NegativeFlagItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableFieldSaveFlag(-1)]
                    private bool ShouldSerializeName() => _name != null;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3006");
    }
}
