using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class VolatileStateTests
{
    private const string TimerClass = """
        using System;
        using ModernUO.Serialization;
        using Server;

        namespace TestNamespace
        {
            [SerializationGenerator(0)]
            public partial class TimerHolder : ISerializable
            {
                [SerializableField(0)]
                [DeserializeTimer(nameof(RestartTimer))]
                private Timer _timer;

                private void RestartTimer(TimeSpan delay) { }

                public Serial Serial => default;
                public void MarkDirty() { }
            }
        }
        """;

    [Fact]
    public void SerializedTimer_EmitsVolatileAttributeAndStopHelper()
    {
        var (diagnostics, generated) = SourceGeneratorTestHelper.RunGenerator(TimerClass);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.NotNull(generated);
        Assert.Contains("[ModernUO.Serialization.VolatileSerializedState(ModernUO.Serialization.VolatileReason.SerializedTimer)]", generated);
        Assert.Contains("public void StopTimer()", generated);
        Assert.Contains("_timer.Stop();", generated);
        Assert.Contains("_timer = null;", generated);
    }

    [Fact]
    public void DeltaDateTime_EmitsVolatileAttribute()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class DeltaHolder : ISerializable
                {
                    [SerializableField(0)]
                    [DeltaDateTime]
                    private DateTime _expires;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (_, generated) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.NotNull(generated);
        Assert.Contains("VolatileSerializedState(ModernUO.Serialization.VolatileReason.DeltaDateTime)", generated);
    }

    [Fact]
    public void PlainClass_EmitsNoVolatileAttribute()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class Plain : ISerializable
                {
                    [SerializableField(0)]
                    private int _value;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (_, generated) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.NotNull(generated);
        Assert.DoesNotContain("VolatileSerializedState", generated);
    }

    [Fact]
    public void HandDeclaredVolatile_IsNotDuplicated()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [VolatileSerializedState(VolatileReason.Declared)]
                [SerializationGenerator(0)]
                public partial class DeclaredVolatile : ISerializable
                {
                    [SerializableField(0)]
                    [DeserializeTimer(nameof(RestartTimer))]
                    private Timer _timer;

                    private void RestartTimer(TimeSpan delay) { }

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generated) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.NotNull(generated);
        Assert.DoesNotContain("VolatileSerializedState", generated);
    }
}
