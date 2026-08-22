using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Name-based linkage: field-side and method-side spellings must generate identical code,
/// and broken linkage must be reported instead of silently generating garbage.
/// </summary>
public class LinkageTests
{
    private const string MethodSideSource = """
        using System;
        using ModernUO.Serialization;
        using Server;

        namespace Server.TestContent
        {
            [SerializationGenerator(0)]
            public partial class LinkedItem : ISerializable
            {
                [SerializableField(0)]
                private int _charges;

                [SerializableFieldSaveFlag(nameof(_charges))]
                private bool ShouldSerializeCharges() => _charges != 8;

                [SerializableFieldDefault(nameof(_charges))]
                private int ChargesDefaultValue() => 8;

                [SerializableField(1)]
                private int _level;

                [SerializableFieldChanged(nameof(_level))]
                private void OnLevelChanged(int oldValue, int newValue)
                {
                }

                public DateTime Created { get; set; }
                public Serial Serial { get; }
                public bool Deleted => false;
                public void Delete() { }
            }
        }
        """;

    private const string FieldSideSource = """
        using System;
        using ModernUO.Serialization;
        using Server;

        namespace Server.TestContent
        {
            [SerializationGenerator(0)]
            public partial class LinkedItem : ISerializable
            {
                [SerializableField(0)]
                [SaveFlag(nameof(ShouldSerializeCharges), nameof(ChargesDefaultValue))]
                private int _charges;

                private bool ShouldSerializeCharges() => _charges != 8;

                private int ChargesDefaultValue() => 8;

                [SerializableField(1)]
                [FieldChanged(nameof(OnLevelChanged))]
                private int _level;

                private void OnLevelChanged(int oldValue, int newValue)
                {
                }

                public DateTime Created { get; set; }
                public Serial Serial { get; }
                public bool Deleted => false;
                public void Delete() { }
            }
        }
        """;

    [Fact]
    public void FieldSideAndMethodSideLinkage_GenerateIdenticalCode()
    {
        var (methodDiags, methodSource) = SourceGeneratorTestHelper.RunGenerator(MethodSideSource);
        var (fieldDiags, fieldSource) = SourceGeneratorTestHelper.RunGenerator(FieldSideSource);

        Assert.Empty(methodDiags.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.Empty(fieldDiags.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.Equal(methodSource, fieldSource);
    }

    [Fact]
    public void ConflictingLinkageStyles_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class ConflictedItem : ISerializable
                {
                    [SerializableField(0)]
                    [SaveFlag(nameof(ShouldSerializeCharges))]
                    private int _charges;

                    private bool ShouldSerializeCharges() => _charges != 8;

                    [SerializableFieldSaveFlag(nameof(_charges))]
                    private bool AlsoShouldSerializeCharges() => _charges != 8;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3016");
    }

    [Fact]
    public void FieldSideLinkage_MissingMethod_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class MissingMethodItem : ISerializable
                {
                    [SerializableField(0)]
                    [SaveFlag("DoesNotExist")]
                    private int _charges;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3015");
    }

    [Fact]
    public void MethodSideSaveFlag_WrongSignature_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class WrongShapeItem : ISerializable
                {
                    [SerializableField(0)]
                    private int _charges;

                    [SerializableFieldSaveFlag(nameof(_charges))]
                    private int ShouldSerializeCharges() => _charges;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3015");
    }

    [Fact]
    public void DefaultWithoutSaveFlag_ReportsWarning()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class DanglingDefaultItem : ISerializable
                {
                    [SerializableField(0)]
                    private int _charges;

                    [SerializableFieldDefault(nameof(_charges))]
                    private int ChargesDefaultValue() => 8;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.NotNull(generatedSource);
        Assert.Contains(diagnostics, d => d.Id == "SG3017");
    }

    [Fact]
    public void DeserializeTimer_MissingMethod_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class BrokenTimerItem : ISerializable
                {
                    [SerializableField(0)]
                    [DeserializeTimer("DoesNotExist")]
                    private Timer _timer;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3015");
    }

    [Fact]
    public void SerializableTimer_WithoutDeserializeTimer_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class NakedTimerItem : ISerializable
                {
                    [SerializableField(0)]
                    private Timer _timer;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3008");
    }
}
