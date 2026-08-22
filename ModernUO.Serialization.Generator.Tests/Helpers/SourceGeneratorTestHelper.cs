using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ModernUO.Serialization.Generator;

namespace ModernUO.Serialization.Generator.Tests.Helpers;

public static class SourceGeneratorTestHelper
{
    // Stub code for Server interfaces needed by the generator
    public const string ServerStubs = """
        namespace Server
        {
            public interface IGenericReader
            {
                string ReadString(bool intern = false);
                int ReadInt();
                uint ReadUInt();
                long ReadLong();
                ulong ReadULong();
                short ReadShort();
                ushort ReadUShort();
                byte ReadByte();
                sbyte ReadSByte();
                bool ReadBool();
                float ReadFloat();
                double ReadDouble();
                decimal ReadDecimal();
                System.DateTime ReadDateTime();
                System.DateTime ReadDeltaTime();
                System.TimeSpan ReadTimeSpan();
                System.Guid ReadGuid();
                int ReadEncodedInt();
                T ReadEnum<T>() where T : struct, System.Enum;
                Serial ReadSerial();
                Point2D ReadPoint2D();
                Point3D ReadPoint3D();
                Rectangle2D ReadRect2D();
                Rectangle3D ReadRect3D();
            }

            public interface IGenericWriter
            {
                void Write(string value);
                void Write(int value);
                void Write(uint value);
                void Write(long value);
                void Write(ulong value);
                void Write(short value);
                void Write(ushort value);
                void Write(byte value);
                void Write(sbyte value);
                void Write(bool value);
                void Write(float value);
                void Write(double value);
                void Write(decimal value);
                void Write(System.DateTime value);
                void WriteDeltaTime(System.DateTime value);
                void Write(System.TimeSpan value);
                void Write(System.Guid value);
                void WriteEncodedInt(int value);
                void Write<T>(T value) where T : struct, System.Enum;
                void WriteEnum<T>(T value) where T : struct, System.Enum;
                void Write(Serial value);
                void Write(Point2D value);
                void Write(Point3D value);
                void Write(Rectangle2D value);
                void Write(Rectangle3D value);
            }

            public interface IGenericSerializable
            {
                void Serialize(IGenericWriter writer);
            }

            public interface ISerializable : IGenericSerializable
            {
                System.DateTime Created { get; set; }
                Serial Serial { get; }
                void Deserialize(IGenericReader reader);
                bool Deleted { get; }
                void Delete();
            }

            public static class ISerializableExtensions
            {
                public static void MarkDirty(this ISerializable entity) { }
            }

            public static class Utility
            {
                public static void Tidy<T>(this System.Collections.Generic.List<T> list) where T : ISerializable { }
                public static void Tidy<T>(this System.Collections.Generic.HashSet<T> set) where T : ISerializable { }
                public static void Tidy<K, V>(this System.Collections.Generic.Dictionary<K, V> dictionary) { }
            }

            public readonly struct Serial
            {
                public readonly uint Value;
                public Serial(uint value) => Value = value;
                public static bool operator ==(Serial l, Serial r) => l.Value == r.Value;
                public static bool operator !=(Serial l, Serial r) => l.Value != r.Value;
                public override bool Equals(object obj) => obj is Serial other && Value == other.Value;
                public override int GetHashCode() => (int)Value;
            }

            public struct Point2D
            {
                public int X, Y;
                public static bool operator ==(Point2D l, Point2D r) => l.X == r.X && l.Y == r.Y;
                public static bool operator !=(Point2D l, Point2D r) => !(l == r);
                public override bool Equals(object obj) => obj is Point2D other && this == other;
                public override int GetHashCode() => X ^ Y;
            }

            public struct Point3D
            {
                public int X, Y, Z;
                public static bool operator ==(Point3D l, Point3D r) => l.X == r.X && l.Y == r.Y && l.Z == r.Z;
                public static bool operator !=(Point3D l, Point3D r) => !(l == r);
                public override bool Equals(object obj) => obj is Point3D other && this == other;
                public override int GetHashCode() => X ^ Y ^ Z;
            }

            public struct Rectangle2D
            {
                public Point2D Start, End;
                public static bool operator ==(Rectangle2D l, Rectangle2D r) => l.Start == r.Start && l.End == r.End;
                public static bool operator !=(Rectangle2D l, Rectangle2D r) => !(l == r);
                public override bool Equals(object obj) => obj is Rectangle2D other && this == other;
                public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();
            }

            public struct Rectangle3D
            {
                public Point3D Start, End;
                public static bool operator ==(Rectangle3D l, Rectangle3D r) => l.Start == r.Start && l.End == r.End;
                public static bool operator !=(Rectangle3D l, Rectangle3D r) => !(l == r);
                public override bool Equals(object obj) => obj is Rectangle3D other && this == other;
                public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();
            }

            public class TextDefinition
            {
                public int Number { get; set; }
                public string String { get; set; }
            }

            public class Poison
            {
                public int Level { get; set; }
            }

            public class Race
            {
                public int Id { get; set; }
            }

            public class Map
            {
                public int Id { get; set; }
            }

            public class Timer
            {
                public System.TimeSpan Delay { get; set; }
                public System.DateTime Next { get; set; }
            }

            public static class Core
            {
                public static System.DateTime Now => System.DateTime.UtcNow;
            }
        }
        """;

    public static (ImmutableArray<Diagnostic> Diagnostics, string? GeneratedSource) RunGenerator(
        string sourceCode,
        string[]? additionalSources = null,
        IEnumerable<(string fileName, string content)>? additionalTexts = null)
    {
        var syntaxTrees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(sourceCode),
            CSharpSyntaxTree.ParseText(ServerStubs)
        };

        if (additionalSources != null)
        {
            foreach (var additional in additionalSources)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(additional));
            }
        }

        // Get all the runtime references needed for compilation
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        var references = trustedAssemblies
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => MetadataReference.CreateFromFile(p))
            .Cast<MetadataReference>()
            .Concat([MetadataReference.CreateFromFile(typeof(SerializationGeneratorAttribute).Assembly.Location)])
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new EntitySerializationGenerator();

        var additionalTextsList = new List<AdditionalText>();
        if (additionalTexts != null)
        {
            foreach (var (fileName, content) in additionalTexts)
            {
                additionalTextsList.Add(new InMemoryAdditionalText(fileName, content));
            }
        }

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.CreateRange(additionalTextsList));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var generatedSource = outputCompilation.SyntaxTrees
            .FirstOrDefault(st => st.FilePath.EndsWith(".Serialization.g.cs"))
            ?.GetText()
            .ToString();

        return (diagnostics, generatedSource);
    }

    /// <summary>
    /// Runs the generator and returns every generated source, keyed by file name, along with
    /// generator diagnostics and any compile errors in the output compilation. Used by the
    /// snapshot tests to pin exact output.
    /// </summary>
    public static (
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<(string FileName, string Content)> Sources,
        ImmutableArray<Diagnostic> CompileErrors
    ) RunGeneratorAllOutputs(
        string sourceCode,
        IEnumerable<(string fileName, string content)>? additionalTexts = null)
    {
        var syntaxTrees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(sourceCode),
            CSharpSyntaxTree.ParseText(ServerStubs)
        };

        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        var references = trustedAssemblies
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => MetadataReference.CreateFromFile(p))
            .Cast<MetadataReference>()
            .Concat([MetadataReference.CreateFromFile(typeof(SerializationGeneratorAttribute).Assembly.Location)])
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new EntitySerializationGenerator();

        var additionalTextsList = new List<AdditionalText>();
        if (additionalTexts != null)
        {
            foreach (var (fileName, content) in additionalTexts)
            {
                additionalTextsList.Add(new InMemoryAdditionalText(fileName, content));
            }
        }

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.CreateRange(additionalTextsList));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var sources = outputCompilation.SyntaxTrees
            .Where(st => st.FilePath.EndsWith(".Serialization.g.cs"))
            .Select(st => (Path.GetFileName(st.FilePath), st.GetText().ToString()))
            .ToImmutableArray();

        var compileErrors = outputCompilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        return (diagnostics, sources, compileErrors);
    }

    public static bool HasDiagnostic(ImmutableArray<Diagnostic> diagnostics, string diagnosticId)
    {
        return diagnostics.Any(d => d.Id == diagnosticId);
    }

    public static Diagnostic? GetDiagnostic(ImmutableArray<Diagnostic> diagnostics, string diagnosticId)
    {
        return diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
    }

    public static bool GeneratedSourceContains(string? generatedSource, string text)
    {
        return generatedSource?.Contains(text) ?? false;
    }

    public static bool GeneratedSourceContainsAll(string? generatedSource, params string[] texts)
    {
        if (generatedSource == null) return false;
        return texts.All(t => generatedSource.Contains(t));
    }
}

public class InMemoryAdditionalText : AdditionalText
{
    private readonly string _path;
    private readonly string _content;

    public InMemoryAdditionalText(string path, string content)
    {
        _path = path;
        _content = content;
    }

    public override string Path => _path;

    public override SourceText? GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(_content, Encoding.UTF8);
    }
}
