using System.Text;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// A migration content struct reads a stream written by the live generator of an older version, so
/// its save flag enums must be sized and partitioned exactly like the live SaveFlag enums were:
/// int for up to 32 flags, ulong for 33-64, and several ulong enums past 64.
/// </summary>
public class MigrationSaveFlagTests
{
    // Binary reader/writer that sizes enums by their underlying type, like ModernUO's, so a width
    // mismatch between writer and content struct misaligns the stream instead of going unnoticed.
    private const string BinaryStreams = """
        namespace Server.TestContent
        {
            using System;
            using System.IO;
            using System.Runtime.CompilerServices;

            public sealed class BinaryGenericWriter : Server.IGenericWriter
            {
                private readonly BinaryWriter _writer;
                public BinaryGenericWriter(Stream stream) => _writer = new BinaryWriter(stream);
                public void Flush() => _writer.Flush();

                public void Write(string value)
                {
                    _writer.Write(value != null);
                    if (value != null) _writer.Write(value);
                }

                public void Write(int value) => _writer.Write(value);
                public void Write(uint value) => _writer.Write(value);
                public void Write(long value) => _writer.Write(value);
                public void Write(ulong value) => _writer.Write(value);
                public void Write(short value) => _writer.Write(value);
                public void Write(ushort value) => _writer.Write(value);
                public void Write(byte value) => _writer.Write(value);
                public void Write(sbyte value) => _writer.Write(value);
                public void Write(bool value) => _writer.Write(value);
                public void Write(float value) => _writer.Write(value);
                public void Write(double value) => _writer.Write(value);
                public void Write(decimal value) => _writer.Write(value);
                public void Write(DateTime value) => throw new NotSupportedException();
                public void WriteDeltaTime(DateTime value) => throw new NotSupportedException();
                public void WriteAnchoredTime(DateTime value) => throw new NotSupportedException();
                public void Write(TimeSpan value) => throw new NotSupportedException();
                public void Write(Guid value) => throw new NotSupportedException();
                public void WriteEncodedInt(int value) => _writer.Write7BitEncodedInt(value);
                public void Write<T>(T value) where T : struct, Enum => WriteEnum(value);

                public void WriteEnum<T>(T value) where T : struct, Enum
                {
                    switch (Unsafe.SizeOf<T>())
                    {
                        case 1: _writer.Write(Unsafe.As<T, byte>(ref value)); break;
                        case 2: _writer.Write(Unsafe.As<T, ushort>(ref value)); break;
                        case 4: _writer.Write(Unsafe.As<T, uint>(ref value)); break;
                        default: _writer.Write(Unsafe.As<T, ulong>(ref value)); break;
                    }
                }

                public void Write(Serial value) => throw new NotSupportedException();
                public void Write(Point2D value) => throw new NotSupportedException();
                public void Write(Point3D value) => throw new NotSupportedException();
                public void Write(Rectangle2D value) => throw new NotSupportedException();
                public void Write(Rectangle3D value) => throw new NotSupportedException();
            }

            public sealed class BinaryGenericReader : Server.IGenericReader
            {
                private readonly BinaryReader _reader;
                public BinaryGenericReader(Stream stream) => _reader = new BinaryReader(stream);
                public bool AtEnd => _reader.BaseStream.Position == _reader.BaseStream.Length;

                public string ReadString(bool intern = false) => _reader.ReadBoolean() ? _reader.ReadString() : null;
                public int ReadInt() => _reader.ReadInt32();
                public uint ReadUInt() => _reader.ReadUInt32();
                public long ReadLong() => _reader.ReadInt64();
                public ulong ReadULong() => _reader.ReadUInt64();
                public short ReadShort() => _reader.ReadInt16();
                public ushort ReadUShort() => _reader.ReadUInt16();
                public byte ReadByte() => _reader.ReadByte();
                public sbyte ReadSByte() => _reader.ReadSByte();
                public bool ReadBool() => _reader.ReadBoolean();
                public float ReadFloat() => _reader.ReadSingle();
                public double ReadDouble() => _reader.ReadDouble();
                public decimal ReadDecimal() => _reader.ReadDecimal();
                public DateTime ReadDateTime() => throw new NotSupportedException();
                public DateTime ReadDeltaTime() => throw new NotSupportedException();
                public DateTime ReadAnchoredTime() => throw new NotSupportedException();
                public TimeSpan ReadTimeSpan() => throw new NotSupportedException();
                public Guid ReadGuid() => throw new NotSupportedException();
                public int ReadEncodedInt() => _reader.Read7BitEncodedInt();

                public T ReadEnum<T>() where T : struct, Enum
                {
                    T value = default;
                    switch (Unsafe.SizeOf<T>())
                    {
                        case 1: Unsafe.As<T, byte>(ref value) = _reader.ReadByte(); break;
                        case 2: Unsafe.As<T, ushort>(ref value) = _reader.ReadUInt16(); break;
                        case 4: Unsafe.As<T, uint>(ref value) = _reader.ReadUInt32(); break;
                        default: Unsafe.As<T, ulong>(ref value) = _reader.ReadUInt64(); break;
                    }

                    return value;
                }

                public Serial ReadSerial() => throw new NotSupportedException();
                public Point2D ReadPoint2D() => throw new NotSupportedException();
                public Point3D ReadPoint3D() => throw new NotSupportedException();
                public Rectangle2D ReadRect2D() => throw new NotSupportedException();
                public Rectangle3D ReadRect3D() => throw new NotSupportedException();
            }
        }
        """;

    // Field i holds i + 1 when present; 0 (the default) is skipped by its save flag.
    private static bool IsPresent(int index, int flagCount) =>
        index is 0 or 1 or 30 or 31 or 32 or 33 or 62 or 63 or 64 or 65 || index == flagCount - 1;

    [Theory]
    [InlineData(20)] // int SaveFlag
    [InlineData(40)] // one ulong SaveFlag; bits 31 and 32 straddle the int boundary
    [InlineData(70)] // SaveFlag + SaveFlag2; bits 63 and 64 straddle the enum boundary
    public void MigrateFrom_ReadsEveryFieldWrittenByTheLiveGenerator(int flagCount)
    {
        var bytes = WriteV0(flagCount);
        var migrated = ReadAsV1(flagCount, bytes);

        var expected = new StringBuilder();
        for (var i = 0; i < flagCount; i++)
        {
            expected.Append(IsPresent(i, flagCount) ? $"{i + 1}" : "null").Append(',');
        }

        expected.Append("True");

        Assert.Equal($"tag|{expected}", migrated);
    }

    private static byte[] WriteV0(int flagCount)
    {
        var source = new StringBuilder();
        source.AppendLine("using ModernUO.Serialization;");
        source.AppendLine("namespace Server.TestContent {");
        source.AppendLine("[SerializationGenerator(0)]");
        source.AppendLine("public partial class WideFlagsItem : Server.ISerializable {");
        AppendFields(source, flagCount);
        AppendEntityMembers(source);

        source.AppendLine("public static byte[] WriteSample() {");
        source.AppendLine("var item = new WideFlagsItem { _tag = \"tag\" };");
        for (var i = 0; i < flagCount; i++)
        {
            if (IsPresent(i, flagCount))
            {
                source.AppendLine($"item._f{i} = {i + 1};");
            }
        }

        source.AppendLine("item._flag = true;");
        source.AppendLine("var stream = new System.IO.MemoryStream();");
        source.AppendLine("var writer = new BinaryGenericWriter(stream);");
        source.AppendLine("item.Serialize(writer);");
        source.AppendLine("writer.Flush();");
        source.AppendLine("return stream.ToArray();");
        source.AppendLine("}");
        source.AppendLine("}");
        source.AppendLine("}");

        var assembly = SourceGeneratorTestHelper.CompileAndLoad(
            $"WideFlagsV0_{flagCount}",
            source + BinaryStreams
        );

        return (byte[])assembly.GetType("Server.TestContent.WideFlagsItem")!
            .GetMethod("WriteSample")!
            .Invoke(null, null)!;
    }

    private static string ReadAsV1(int flagCount, byte[] bytes)
    {
        var source = new StringBuilder();
        source.AppendLine("using ModernUO.Serialization;");
        source.AppendLine("namespace Server.TestContent {");
        source.AppendLine("[SerializationGenerator(1)]");
        source.AppendLine("public partial class WideFlagsItem : Server.ISerializable {");
        source.AppendLine("[SerializableField(0)] private string _migrated;");
        AppendEntityMembers(source);

        source.AppendLine("private void MigrateFrom(V0Content content) {");
        source.AppendLine("var sb = new System.Text.StringBuilder();");
        source.AppendLine("sb.Append(content.Tag).Append('|');");
        for (var i = 0; i < flagCount; i++)
        {
            source.AppendLine($"sb.Append(content.F{i}?.ToString() ?? \"null\").Append(',');");
        }

        source.AppendLine("sb.Append(content.Flag);");
        source.AppendLine("_migrated = sb.ToString();");
        source.AppendLine("}");

        source.AppendLine("public static string ReadSample(byte[] bytes) {");
        source.AppendLine("var reader = new BinaryGenericReader(new System.IO.MemoryStream(bytes));");
        source.AppendLine("var item = new WideFlagsItem();");
        source.AppendLine("item.Deserialize(reader);");
        source.AppendLine("if (!reader.AtEnd) throw new System.InvalidOperationException(\"Unread bytes remain\");");
        source.AppendLine("return item._migrated;");
        source.AppendLine("}");
        source.AppendLine("}");
        source.AppendLine("}");

        var assembly = SourceGeneratorTestHelper.CompileAndLoad(
            $"WideFlagsV1_{flagCount}",
            source + BinaryStreams,
            [("Server.TestContent.WideFlagsItem.v0.json", MigrationJson(flagCount))]
        );

        return (string)assembly.GetType("Server.TestContent.WideFlagsItem")!
            .GetMethod("ReadSample")!
            .Invoke(null, [bytes])!;
    }

    // One unflagged string, flagCount flagged ints, then a flagged bool (stored in its bit only).
    private static void AppendFields(StringBuilder source, int flagCount)
    {
        source.AppendLine("[SerializableField(0)] private string _tag;");
        for (var i = 0; i < flagCount; i++)
        {
            source.AppendLine($"[SerializableField({i + 1})] [SaveFlag(nameof(ShouldSerializeF{i}))] private int _f{i};");
            source.AppendLine($"private bool ShouldSerializeF{i}() => _f{i} != 0;");
        }

        source.AppendLine($"[SerializableField({flagCount + 1})] [SaveFlag(nameof(ShouldSerializeFlag))] private bool _flag;");
        source.AppendLine("private bool ShouldSerializeFlag() => _flag;");
    }

    private static void AppendEntityMembers(StringBuilder source)
    {
        source.AppendLine("public WideFlagsItem() { }");
        source.AppendLine("public System.DateTime Created { get; set; }");
        source.AppendLine("public Server.Serial Serial { get; }");
        source.AppendLine("public bool Deleted => false;");
        source.AppendLine("public void Delete() { }");
    }

    private static string MigrationJson(int flagCount)
    {
        var properties = new List<string>
        {
            """{ "name": "Tag", "type": "string", "rule": "PrimitiveTypeMigrationRule" }"""
        };

        for (var i = 0; i < flagCount; i++)
        {
            properties.Add(
                $$"""{ "name": "F{{i}}", "type": "int", "usesSaveFlag": true, "rule": "PrimitiveTypeMigrationRule" }"""
            );
        }

        properties.Add(
            """{ "name": "Flag", "type": "bool", "usesSaveFlag": true, "rule": "PrimitiveTypeMigrationRule" }"""
        );

        return $$"""
            {
                "version": 0,
                "type": "Server.TestContent.WideFlagsItem",
                "properties": [
                    {{string.Join(",\n", properties)}}
                ]
            }
            """;
    }
}
