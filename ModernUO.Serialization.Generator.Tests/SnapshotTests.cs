using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Pins the exact generated output for a corpus of feature fixtures under Snapshots/.
/// Each fixture directory contains an Input.cs, optional {Type}.vN.json migration files,
/// and an Expected/ directory with one file per generated source. A fixture must produce
/// output that compiles and matches its Expected/ files byte for byte.
/// <para>
/// Set UPDATE_SNAPSHOTS=1 to rewrite the Expected/ directories from current output
/// instead of asserting.
/// </para>
/// </summary>
public class SnapshotTests
{
    private static string SnapshotsRoot([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "Snapshots");

    private static bool UpdateMode => Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1";

    // The emitted header stamps the generator version; normalize it so version bumps do not
    // invalidate every snapshot. Line endings are normalized so autocrlf checkouts compare
    // equal.
    private static string NormalizeVersion(string content) =>
        Regex.Replace(
            content.Replace("\r\n", "\n"),
            """(Version: |"ModernUO\.Serialization\.Generator", ")\d+\.\d+\.\d+\.\d+""",
            "$1{VERSION}"
        );

    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();
        foreach (var dir in Directory.GetDirectories(SnapshotsRoot()))
        {
            data.Add(Path.GetFileName(dir));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Fixture_GeneratesPinnedOutput(string fixture)
    {
        var fixtureDir = Path.Combine(SnapshotsRoot(), fixture);
        var source = File.ReadAllText(Path.Combine(fixtureDir, "Input.cs"));

        var additionalTexts = Directory
            .GetFiles(fixtureDir, "*.json")
            .Select(path => (Path.GetFileName(path), File.ReadAllText(path)))
            .ToList();

        var (diagnostics, sources, compileErrors) =
            SourceGeneratorTestHelper.RunGeneratorAllOutputs(source, additionalTexts);

        var expectedDir = Path.Combine(fixtureDir, "Expected");

        if (UpdateMode)
        {
            if (Directory.Exists(expectedDir))
            {
                Directory.Delete(expectedDir, true);
            }

            Directory.CreateDirectory(expectedDir);

            foreach (var (fileName, content) in sources)
            {
                File.WriteAllText(Path.Combine(expectedDir, fileName), NormalizeVersion(content));
            }
        }

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, $"Generator errors: {string.Join("\n", errors)}");

        // A KnownBroken.txt marker documents a defect whose (non-compiling) output is still
        // pinned so the fix shows up as a snapshot diff. Remove the marker with the fix.
        if (!File.Exists(Path.Combine(fixtureDir, "KnownBroken.txt")))
        {
            Assert.True(compileErrors.Length == 0, $"Output does not compile: {string.Join("\n", compileErrors.Take(10))}");
        }

        Assert.True(sources.Length > 0, "Fixture produced no generated sources.");

        if (UpdateMode)
        {
            return;
        }

        Assert.True(Directory.Exists(expectedDir), $"Missing {expectedDir}; run with UPDATE_SNAPSHOTS=1 to create it.");

        var expectedFiles = Directory
            .GetFiles(expectedDir)
            .Select(Path.GetFileName)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        var actualFiles = sources
            .Select(s => s.FileName)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFiles, actualFiles);

        foreach (var (fileName, content) in sources)
        {
            var expected = File.ReadAllText(Path.Combine(expectedDir, fileName)).Replace("\r\n", "\n");
            Assert.Equal(expected, NormalizeVersion(content));
        }
    }
}
