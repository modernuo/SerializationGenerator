using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Nullable&lt;T&gt; fields (NullableMigrationRule) round trip through the live generator, both as
/// fields and nested in collections, and through a migration content struct. A trailing string and an
/// unread-bytes check prove the HasValue prefixes keep the stream aligned.
/// </summary>
public class NullableValueTypeTests
{
    private const string EntityMembers = """
        public System.DateTime Created { get; set; }
        public Server.Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
        """;

    private const string LiveEntity = $$"""
        using System;
        using System.Collections.Generic;
        using ModernUO.Serialization;
        namespace Server.TestContent {
        public enum Mood { Calm, Angry }

        [SerializationGenerator(0)]
        public partial struct Stats
        {
            [SerializableField(0)] private int _strength;
        }

        [SerializationGenerator(0)]
        public partial class NullableValuesItem : Server.ISerializable {
        [SerializableField(0)] private int? _count;
        [SerializableField(1)] [EncodedInt] private int? _encoded;
        [SerializableField(2)] private Mood? _mood;
        [SerializableField(3)] private TimeSpan? _cooldown;
        [SerializableField(4)] private Stats? _stats;
        [SerializableField(5)] private List<int?> _samples;
        [SerializableField(6)] private Dictionary<int, double?> _weights;
        [SerializableField(7)] [SaveFlag(nameof(ShouldSerializeBonus))] private int? _bonus;
        private bool ShouldSerializeBonus() => _bonus != null;
        [SerializableField(8)] private string _tail;

        public NullableValuesItem() { }
        {{EntityMembers}}

        private static string Show<T>(T? value) where T : struct => value?.ToString() ?? "null";

        private string Describe() =>
            $"{Show(_count)},{Show(_encoded)},{Show(_mood)},{Show(_cooldown)},{Show(_stats?.Strength)}," +
            $"[{string.Join(",", _samples.ConvertAll(Show))}],{Show(_weights[1])},{Show(_bonus)},{_tail}";

        public static string RoundTrip(bool populated) {
        var item = new NullableValuesItem { _samples = [], _weights = new() { [1] = null }, _tail = "end" };
        if (populated) {
        item._count = -7;
        item._encoded = 300;
        item._mood = Server.TestContent.Mood.Angry;
        item._cooldown = TimeSpan.FromSeconds(90);
        item._stats = new Stats { Strength = 12 };
        item._samples = [1, null, 3];
        item._weights[1] = 2.5;
        item._bonus = 4;
        }

        var stream = new System.IO.MemoryStream();
        var writer = new BinaryGenericWriter(stream);
        item.Serialize(writer);
        writer.Flush();

        var reader = new BinaryGenericReader(new System.IO.MemoryStream(stream.ToArray()));
        var copy = new NullableValuesItem();
        copy.Deserialize(reader);
        if (!reader.AtEnd) throw new InvalidOperationException("Unread bytes remain");
        return copy.Describe();
        }
        }
        }
        """;

    [Theory]
    [InlineData(true, "-7,300,Angry,00:01:30,12,[1,null,3],2.5,4,end")]
    [InlineData(false, "null,null,null,null,null,[],null,null,end")]
    public void LiveRoundTrip_PreservesValuesAndNulls(bool populated, string expected)
    {
        var assembly = SourceGeneratorTestHelper.CompileAndLoad(
            $"NullableValuesLive_{populated}",
            LiveEntity + MigrationSaveFlagTests.BinaryStreams
        );

        var result = (string)assembly.GetType("Server.TestContent.NullableValuesItem")!
            .GetMethod("RoundTrip")!
            .Invoke(null, [populated])!;

        Assert.Equal(expected, result);
    }

    // The v1 side reads the schema that schema mode generates for the v0 entity, as the tool would.
    [Theory]
    [InlineData(true, "5|6|end")]
    [InlineData(false, "null|null|end")]
    public void MigrateFrom_ReadsNullableFieldsWrittenByTheLiveGenerator(bool populated, string expected)
    {
        var v0Source = V0Source(populated);
        var schemas = SourceGeneratorTestHelper.GenerateMigrationSchemas($"MigratingNullableSchema_{populated}", v0Source);

        Assert.Contains(
            "\"rule\": \"NullableMigrationRule\"",
            Assert.Single(schemas, s => s.fileName == "Server.TestContent.MigratingNullableItem.v0.json").content
        );

        var bytes = WriteV0(populated, v0Source);
        Assert.Equal(expected, ReadAsV1(populated, bytes, schemas));
    }

    private static string V0Source(bool populated)
    {
        var values = populated ? "_count = 5, _bonus = 6," : "";
        return $$"""
            using ModernUO.Serialization;
            namespace Server.TestContent {
            [SerializationGenerator(0)]
            public partial class MigratingNullableItem : Server.ISerializable {
            [SerializableField(0)] private int? _count;
            [SerializableField(1)] [SaveFlag(nameof(ShouldSerializeBonus))] private int? _bonus;
            private bool ShouldSerializeBonus() => _bonus != null;
            [SerializableField(2)] private string _tail;

            public MigratingNullableItem() { }
            {{EntityMembers}}

            public static byte[] WriteSample() {
            var item = new MigratingNullableItem { {{values}} _tail = "end" };
            var stream = new System.IO.MemoryStream();
            var writer = new BinaryGenericWriter(stream);
            item.Serialize(writer);
            writer.Flush();
            return stream.ToArray();
            }
            }
            }
            """ + MigrationSaveFlagTests.BinaryStreams;
    }

    private static byte[] WriteV0(bool populated, string v0Source)
    {
        var assembly = SourceGeneratorTestHelper.CompileAndLoad($"MigratingNullableV0_{populated}", v0Source);

        return (byte[])assembly.GetType("Server.TestContent.MigratingNullableItem")!
            .GetMethod("WriteSample")!
            .Invoke(null, null)!;
    }

    private static string ReadAsV1(bool populated, byte[] bytes, List<(string fileName, string content)> schemas)
    {
        var source = $$"""
            using ModernUO.Serialization;
            namespace Server.TestContent {
            [SerializationGenerator(1)]
            public partial class MigratingNullableItem : Server.ISerializable {
            [SerializableField(0)] private string _migrated;

            public MigratingNullableItem() { }
            {{EntityMembers}}

            private void MigrateFrom(V0Content content) {
            _migrated = $"{content.Count?.ToString() ?? "null"}|{content.Bonus?.ToString() ?? "null"}|{content.Tail}";
            }

            public static string ReadSample(byte[] bytes) {
            var reader = new BinaryGenericReader(new System.IO.MemoryStream(bytes));
            var item = new MigratingNullableItem();
            item.Deserialize(reader);
            if (!reader.AtEnd) throw new System.InvalidOperationException("Unread bytes remain");
            return item._migrated;
            }
            }
            }
            """;

        var assembly = SourceGeneratorTestHelper.CompileAndLoad(
            $"MigratingNullableV1_{populated}",
            source + MigrationSaveFlagTests.BinaryStreams,
            schemas
        );

        return (string)assembly.GetType("Server.TestContent.MigratingNullableItem")!
            .GetMethod("ReadSample")!
            .Invoke(null, [bytes])!;
    }
}
