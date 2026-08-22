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

                    [SerializableFieldSaveFlag(nameof(_name))]
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
            sb.AppendLine($"        [SerializableFieldSaveFlag(nameof(_field{i}))]");
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
            sb.AppendLine($"        [SerializableFieldSaveFlag(nameof(_field{i}))]");
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
            sb.AppendLine($"        [SerializableFieldSaveFlag(nameof(_field{i}))]");
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

        // Should have SaveFlag enum (first 64 flags)
        Assert.Matches(EnumSaveFlagRegex(), generatedSource);

        // Should have SaveFlag2 enum (flags 65-70)
        Assert.Matches(EnumSaveFlag2Regex(), generatedSource);

        // Verify correct enum naming - should NOT have duplicate "enum SaveFlag : ulong" without number
        var saveFlagMatches = EnumSaveFlagRegex().Matches(generatedSource);
        var saveFlag2Matches = EnumSaveFlag2Regex().Matches(generatedSource);
        Assert.Single(saveFlagMatches); // Only one "enum SaveFlag : ulong"
        Assert.Single(saveFlag2Matches); // Only one "enum SaveFlag2 : ulong"

        // Verify serialization uses correct enum for fields >= 64
        // Field64 should use saveFlags2 & SaveFlag2.Field64
        Assert.Matches(SerializeSaveFlags2Regex(), generatedSource);

        // Verify deserialization uses correct enum for fields >= 64
        Assert.Matches(DeserializeSaveFlags2Regex(), generatedSource);

        // Verify fields 0-63 use SaveFlag (not SaveFlag2)
        Assert.Contains("saveFlags & SaveFlag.Field0", generatedSource);
        Assert.Contains("saveFlags & SaveFlag.Field63", generatedSource);

        // Verify fields 64+ use SaveFlag2
        Assert.Contains("saveFlags2 & SaveFlag2.Field64", generatedSource);
        Assert.Contains("saveFlags2 & SaveFlag2.Field69", generatedSource);
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

                    [SerializableFieldSaveFlag(nameof(_count))]
                    private bool ShouldSerializeCount() => _count != 0;

                    [SerializableFieldDefault(nameof(_count))]
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

    [GeneratedRegex(@"enum SaveFlag\s*:\s*ulong")]
    private static partial Regex EnumSaveFlagRegex();

    [GeneratedRegex(@"enum SaveFlag2\s*:\s*ulong")]
    private static partial Regex EnumSaveFlag2Regex();

    [GeneratedRegex(@"saveFlags2\s*&\s*SaveFlag2\.Field")]
    private static partial Regex SerializeSaveFlags2Regex();

    [GeneratedRegex(@"saveFlags2\s*&\s*SaveFlag2\.Field")]
    private static partial Regex DeserializeSaveFlags2Regex();
}
