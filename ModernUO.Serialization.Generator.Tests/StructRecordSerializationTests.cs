using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class StructRecordSerializationTests
{
    [Fact]
    public void Struct_WithStaticFactory_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial struct SimpleStruct
                {
                    [SerializableField(0)]
                    private int _value;

                    public static SimpleStruct Deserialize(IGenericReader reader)
                    {
                        return new SimpleStruct();
                    }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial struct SimpleStruct", generatedSource);
        Assert.Contains("void Serialize(", generatedSource);
        // Struct should NOT have a serial constructor
        Assert.DoesNotContain("SimpleStruct(Serial serial)", generatedSource);
    }

    [Fact]
    public void Struct_WithInstanceDeserialize_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial struct SimpleStruct
                {
                    [SerializableField(0)]
                    private int _value;

                    public void Deserialize(IGenericReader reader)
                    {
                    }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial struct SimpleStruct", generatedSource);
    }

    [Fact]
    public void Struct_WithoutDeserializeMethod_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial struct BadStruct
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
    public void Record_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial record SimpleRecord : ISerializable
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
        Assert.Contains("partial record SimpleRecord", generatedSource);
    }

    [Fact]
    public void RecordStruct_WithStaticFactory_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial record struct SimpleRecordStruct
                {
                    [SerializableField(0)]
                    private int _value;

                    public static SimpleRecordStruct Deserialize(IGenericReader reader)
                    {
                        return new SimpleRecordStruct();
                    }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial record struct SimpleRecordStruct", generatedSource);
        // Record struct should NOT have a serial constructor
        Assert.DoesNotContain("SimpleRecordStruct(Serial serial)", generatedSource);
    }

    [Fact]
    public void RecordStruct_WithoutDeserializeMethod_ReportsDiagnostic()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial record struct BadRecordStruct
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
    public void Struct_MultipleFields_GeneratesCorrectly()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial struct MultiFieldStruct
                {
                    [SerializableField(0)]
                    private int _x;

                    [SerializableField(1)]
                    private int _y;

                    [SerializableField(2)]
                    private int _z;

                    public static MultiFieldStruct Deserialize(IGenericReader reader)
                    {
                        return new MultiFieldStruct();
                    }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("public int X", generatedSource);
        Assert.Contains("public int Y", generatedSource);
        Assert.Contains("public int Z", generatedSource);
    }
}
