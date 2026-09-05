using System.Reflection;
using Microsoft.CodeAnalysis;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class DirtyTargetDiagnosticTests
{
    [Fact]
    public void SG3019_SubObjectWithoutDirtyTarget_ReportsWarningAndStillGenerates()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class Untracked
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(1)]
                    private List<int> _values;
                }
            }
            """;

        var (diagnostics, generated) = SourceGeneratorTestHelper.RunGenerator(source);

        var warning = Assert.Single(diagnostics, d => d.Id == "SG3019");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.NotNull(generated);
        Assert.DoesNotContain("    ;", generated);
    }

    [Fact]
    public void SG3019_NotReportedForSerializableEntity()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class TrackedItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SG3019");
    }

    [Fact]
    public void SG3019_NotReportedWithDirtyTrackingEntity()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class TrackedSubObject
                {
                    [DirtyTrackingEntity]
                    private ISerializable _parent;

                    [SerializableField(0)]
                    private string _name;
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SG3019");
    }

    [Fact]
    public void SG3019_NotReportedForValueTypes()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial struct PlainStruct
                {
                    [SerializableField(0)]
                    private int _value;
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SG3019");
    }

    [Fact]
    public void SG3019_NotReportedWhenNothingIsMutable()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ReadOnlyOnly
                {
                    [SerializableField(0)]
                    private readonly string _name;
                }
            }
            """;

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SG3019");
    }

    [Fact]
    public void SG3020_ManualDirtyCheckingOnGeneratedClass_ReportsError()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [ManualDirtyChecking]
                [SerializationGenerator(0)]
                public partial class Conflicted : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generated) = SourceGeneratorTestHelper.RunGenerator(source);

        var error = Assert.Single(diagnostics, d => d.Id == "SG3020");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Null(generated);
    }

    [Fact]
    public void EveryDescriptorIsRegisteredWithDiagnosticInfo()
    {
        var descriptors = typeof(DiagnosticDescriptors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
            .ToList();

        Assert.NotEmpty(descriptors);

        foreach (var descriptor in descriptors)
        {
            // ToDiagnostic throws KeyNotFoundException for an unregistered id.
            var diagnostic = DiagnosticInfo.Create(descriptor, Location.None, "a", "b", "c").ToDiagnostic();
            Assert.Equal(descriptor.Id, diagnostic.Id);
        }
    }
}
