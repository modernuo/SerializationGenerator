using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Name-based linkage: [SaveFlag], [DeserializeTimer], and the fieldChanged argument of
/// [SerializableField] name their companion methods, and broken linkage must be reported
/// instead of silently generating garbage.
/// </summary>
public class LinkageTests
{
    [Fact]
    public void FieldSideLinkage_GeneratesLinkedCode()
    {
        const string source = """
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

                    [SerializableField(1, fieldChanged: nameof(OnLevelChanged))]
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

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("ShouldSerializeCharges()", generatedSource);
        Assert.Contains("ChargesDefaultValue()", generatedSource);
        Assert.Contains("OnLevelChanged(", generatedSource);
    }

    [Fact]
    public void SaveFlag_OnSerializableProperty_GeneratesFlag()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class PropertyFlagItem : ISerializable
                {
                    private string _name;

                    [SerializableProperty(0, useField: nameof(_name))]
                    [SaveFlag(nameof(ShouldSerializeName))]
                    public string Name
                    {
                        get => _name;
                        set => _name = value;
                    }

                    private bool ShouldSerializeName() => _name != null;

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("enum SaveFlag", generatedSource);
        Assert.Contains("Name", generatedSource);
        Assert.Contains("ShouldSerializeName()", generatedSource);
    }

    [Fact]
    public void SaveFlag_MissingMethod_ReportsDiagnostic()
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
    public void SaveFlag_WrongSignature_ReportsDiagnostic()
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
                    [SaveFlag(nameof(ShouldSerializeCharges))]
                    private int _charges;

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
    public void SaveFlag_DefaultWrongSignature_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class WrongDefaultItem : ISerializable
                {
                    [SerializableField(0)]
                    [SaveFlag(nameof(ShouldSerializeCharges), nameof(ChargesDefaultValue))]
                    private int _charges;

                    private bool ShouldSerializeCharges() => _charges != 8;

                    private string ChargesDefaultValue() => "8";

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
    public void FieldChanged_WrongSignature_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class WrongChangedItem : ISerializable
                {
                    [SerializableField(0, fieldChanged: nameof(OnLevelChanged))]
                    private int _level;

                    private void OnLevelChanged(int newValue)
                    {
                    }

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
    public void FieldChanged_OnReadonlyField_ReportsDiagnostic()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace Server.TestContent
            {
                [SerializationGenerator(0)]
                public partial class ReadonlyChangedItem : ISerializable
                {
                    [SerializableField(0, fieldChanged: nameof(OnIdChanged))]
                    private readonly string _id;

                    private void OnIdChanged(string oldValue, string newValue)
                    {
                    }

                    public DateTime Created { get; set; }
                    public Serial Serial { get; }
                    public bool Deleted => false;
                    public void Delete() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "SG3018");
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
