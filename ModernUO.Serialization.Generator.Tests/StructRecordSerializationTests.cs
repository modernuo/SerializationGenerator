using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class StructRecordSerializationTests
{
    [Fact]
    public void Struct_GeneratesSerializeAndDeserialize()
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
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial struct SimpleStruct", generatedSource);
        Assert.Contains("public void Serialize(", generatedSource);
        Assert.Contains("public void Deserialize(", generatedSource);
        // Structs cannot have virtual members
        Assert.DoesNotContain("virtual", generatedSource);
        // Struct should NOT have a serial constructor
        Assert.DoesNotContain("SimpleStruct(Serial serial)", generatedSource);
    }

    // The generator emits Deserialize for value types; a user-declared one collides with it.
    [Fact]
    public void Struct_WithInstanceDeserialize_ReportsDiagnostic()
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
    public void RecordStruct_GeneratesCorrectly()
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
    public void RecordStruct_WithStaticFactory_ReportsDiagnostic()
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
