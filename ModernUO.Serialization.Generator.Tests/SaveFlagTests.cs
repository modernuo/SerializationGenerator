/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SaveFlagTests.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using System.Text.RegularExpressions;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public partial class SaveFlagTests
{
    [Fact]
    public void SingleSaveFlag_GeneratesEnum()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SaveFlagItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableFieldSaveFlag(0)]
                    private bool ShouldSerializeName() => _name != null;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("enum SaveFlag", generatedSource);
        Assert.Contains("None", generatedSource);
        Assert.Contains("Name", generatedSource);
    }

    [Fact]
    public void MultipleSaveFlags_Under32_UsesInt()
    {
        // Generate source with 5 fields, all with save flags
        var sb = new StringBuilder();
        sb.AppendLine("""
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class MultiFlagItem : ISerializable
                {
            """);

        for (int i = 0; i < 5; i++)
        {
            sb.AppendLine($"        [SerializableField({i})]");
            sb.AppendLine($"        private string _field{i};");
            sb.AppendLine();
            sb.AppendLine($"        [SerializableFieldSaveFlag({i})]");
            sb.AppendLine($"        private bool ShouldSerializeField{i}() => _field{i} != null;");
            sb.AppendLine();
        }

        sb.AppendLine("""
                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """);

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(sb.ToString());

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("enum SaveFlag", generatedSource);
        // Should not have : ulong
        Assert.DoesNotContain(": ulong", generatedSource);
        // Should have all 5 field flags
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"Field{i}", generatedSource);
        }
    }

    [Fact]
    public void SaveFlags_Over32_UsesUlong()
    {
        // Generate source with 35 fields, all with save flags
        var sb = new StringBuilder();
        sb.AppendLine("""
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ManyFlagItem : ISerializable
                {
            """);

        for (int i = 0; i < 35; i++)
        {
            sb.AppendLine($"        [SerializableField({i})]");
            sb.AppendLine($"        private int _field{i};");
            sb.AppendLine();
            sb.AppendLine($"        [SerializableFieldSaveFlag({i})]");
            sb.AppendLine($"        private bool ShouldSerializeField{i}() => _field{i} != 0;");
            sb.AppendLine();
        }

        sb.AppendLine("""
                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """);

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(sb.ToString());

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        // Should use ulong
        Assert.Contains(": ulong", generatedSource);
    }

    [Fact]
    public void SaveFlags_Over64_UsesMultipleEnums()
    {
        // Generate source with 70 fields, all with save flags
        var sb = new StringBuilder();
        sb.AppendLine("""
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class HugeFlagItem : ISerializable
                {
            """);

        for (int i = 0; i < 70; i++)
        {
            sb.AppendLine($"        [SerializableField({i})]");
            sb.AppendLine($"        private int _field{i};");
            sb.AppendLine();
            sb.AppendLine($"        [SerializableFieldSaveFlag({i})]");
            sb.AppendLine($"        private bool ShouldSerializeField{i}() => _field{i} != 0;");
            sb.AppendLine();
        }

        sb.AppendLine("""
                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """);

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(sb.ToString());

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        // Should have SaveFlag enum with ulong type (for > 32 flags, uses ulong up to 64)
        // When > 64 flags, should generate multiple enums
        Assert.Contains("enum SaveFlag", generatedSource);
        // For 70 flags, need 2 enums (64 + 6)
        // Check that there are multiple SaveFlag usages (SaveFlag and SaveFlag2 or multiple ulong enums)
        var saveFlagCount = EnumSaveFlagRegex().Count(generatedSource);
        Assert.True(saveFlagCount >= 2, $"Expected at least 2 SaveFlag enums, found {saveFlagCount}");
    }

    [Fact]
    public void SaveFlagWithDefaultValue_GeneratesDefaultLogic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class DefaultValueItem : ISerializable
                {
                    [SerializableField(0)]
                    private int _count;

                    [SerializableFieldSaveFlag(0)]
                    private bool ShouldSerializeCount() => _count != 0;

                    [SerializableFieldDefault(0)]
                    private int GetCountDefault() => 0;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("enum SaveFlag", generatedSource);
        Assert.Contains("GetCountDefault", generatedSource);
    }

    [GeneratedRegex("enum SaveFlag")]
    private static partial Regex EnumSaveFlagRegex();
}
