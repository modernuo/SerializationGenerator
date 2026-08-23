using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class ReadonlyFieldTests
{
    [Fact]
    public void ReadonlyField_GeneratesGetterOnlyProperty()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ReadonlyItem : ISerializable
                {
                    [SerializableField(0)]
                    private readonly string _id;

                    [SerializableField(1)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Readonly field should have a getter-only property (no setter)
        Assert.Contains("public string Id", generatedSource);
        Assert.Contains("get =>", generatedSource);

        // Non-readonly field should have normal property with setter
        Assert.Contains("public string Name", generatedSource);
        Assert.Contains("set", generatedSource);

        // Verify Id has no setter (appears before Name in the source)
        var idIndex = generatedSource.IndexOf("public string Id");
        var nameIndex = generatedSource.IndexOf("public string Name");
        var setterAfterIdIndex = generatedSource.IndexOf("set", idIndex);
        // If there's a setter after Id, it should be after Name's definition (i.e., it's Name's setter, not Id's)
        Assert.True(idIndex < nameIndex, "Id property should appear before Name property");
        Assert.True(setterAfterIdIndex < 0 || setterAfterIdIndex > nameIndex, "Id should not have a setter");
    }

    [Fact]
    public void ReadonlyField_NotSerializedOrDeserialized()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ReadonlyItem : ISerializable
                {
                    [SerializableField(0)]
                    private readonly string _id;

                    [SerializableField(1)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Count occurrences of write/read for name (should exist) but not for id in serialization
        // The readonly _id field should not be written in Serialize
        var serializeMethod = ExtractMethod(generatedSource, "void Serialize");
        Assert.NotNull(serializeMethod);
        Assert.DoesNotContain("_id", serializeMethod);
        Assert.Contains("_name", serializeMethod);

        // The readonly _id field should not be read in Deserialize
        var deserializeMethod = ExtractMethod(generatedSource, "void Deserialize");
        Assert.NotNull(deserializeMethod);
        Assert.DoesNotContain("_id", deserializeMethod);
        Assert.Contains("_name", deserializeMethod);
    }

    [Fact]
    public void ReadonlyField_MixedWithNonReadonly_CorrectOrder()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class MixedItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _first;

                    [SerializableField(1)]
                    private readonly string _readonlyMiddle;

                    [SerializableField(2)]
                    private string _last;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // All properties should be generated
        Assert.Contains("public string First", generatedSource);
        Assert.Contains("public string ReadonlyMiddle", generatedSource);
        Assert.Contains("public string Last", generatedSource);

        // Only non-readonly fields in serialization
        var serializeMethod = ExtractMethod(generatedSource, "void Serialize");
        Assert.Contains("_first", serializeMethod);
        Assert.DoesNotContain("_readonlyMiddle", serializeMethod);
        Assert.Contains("_last", serializeMethod);
    }

    [Fact]
    public void ReadonlyField_WithSaveFlag_FlagExcluded()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ReadonlyFlagItem : ISerializable
                {
                    [SerializableField(0)]
                    [SaveFlag(nameof(ShouldSerializeId))]
                    private readonly string _id;

                    [SerializableField(1)]
                    [SaveFlag(nameof(ShouldSerializeName))]
                    private string _name;

                    private bool ShouldSerializeId() => _id != null;

                    private bool ShouldSerializeName() => _name != null;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Only Name should be in the SaveFlag enum, not Id
        Assert.Contains("enum SaveFlag", generatedSource);
        Assert.Contains("Name", generatedSource);
        // The Id should not be in the save flags since it's readonly
        var enumDef = ExtractEnum(generatedSource, "SaveFlag");
        Assert.DoesNotContain("Id", enumDef);
    }

    [Fact]
    public void AllReadonlyFields_NoSerializationGenerated()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class AllReadonlyItem : ISerializable
                {
                    [SerializableField(0)]
                    private readonly string _id;

                    [SerializableField(1)]
                    private readonly int _version;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);

        // Properties should still be generated (readonly fields get get-only properties)
        Assert.Contains("public string Id", generatedSource);
        Assert.Contains("public int Version", generatedSource);
        // These are get-only properties
        Assert.Contains("get =>", generatedSource);

        // Serialize/Deserialize should be mostly empty (just version handling)
        var serializeMethod = ExtractMethod(generatedSource, "void Serialize");
        Assert.DoesNotContain("_id", serializeMethod);
        Assert.DoesNotContain("_version", serializeMethod);
    }

    private static string? ExtractMethod(string source, string methodSignature)
    {
        var startIndex = source.IndexOf(methodSignature);
        if (startIndex < 0) return null;

        int braceCount = 0;
        int bodyStart = source.IndexOf('{', startIndex);
        if (bodyStart < 0) return null;

        int i = bodyStart;
        do
        {
            if (source[i] == '{') braceCount++;
            else if (source[i] == '}') braceCount--;
            i++;
        } while (braceCount > 0 && i < source.Length);

        return source.Substring(startIndex, i - startIndex);
    }

    private static string? ExtractEnum(string source, string enumName)
    {
        var pattern = $"enum {enumName}";
        var startIndex = source.IndexOf(pattern);
        if (startIndex < 0) return null;

        int braceCount = 0;
        int bodyStart = source.IndexOf('{', startIndex);
        if (bodyStart < 0) return null;

        int i = bodyStart;
        do
        {
            if (source[i] == '{') braceCount++;
            else if (source[i] == '}') braceCount--;
            i++;
        } while (braceCount > 0 && i < source.Length);

        return source.Substring(startIndex, i - startIndex);
    }
}
