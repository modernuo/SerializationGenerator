using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class MigrationFileDiagnosticTests
{
    private const string Source = """
        using System;
        using ModernUO.Serialization;
        using Server;

        namespace Server.TestContent
        {
            [SerializationGenerator(1)]
            public partial class MigratingItem : ISerializable
            {
                [SerializableField(0)]
                private string _name;

                public DateTime Created { get; set; }
                public Serial Serial { get; }
                public bool Deleted => false;
                public void Delete() { }

                private void MigrateFrom(V0Content content)
                {
                    _name = content.Name;
                }
            }
        }
        """;

    private const string MigrationJson = """
        {
            "version": 0,
            "type": "Server.TestContent.MigratingItem",
            "properties": [
                {
                    "name": "Name",
                    "type": "string",
                    "rule": "PrimitiveTypeMigrationRule"
                }
            ]
        }
        """;

    // Two files with the same class and version (e.g. nested migration folders): only one can
    // win, and silently ignoring the other loses data. SG3011 makes the conflict visible.
    [Fact]
    public void DuplicateMigrationFile_ReportsDiagnostic()
    {
        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(
            Source,
            additionalTexts:
            [
                ("a/Server.TestContent.MigratingItem.v0.json", MigrationJson),
                ("b/Server.TestContent.MigratingItem.v0.json", MigrationJson)
            ]
        );

        Assert.True(
            SourceGeneratorTestHelper.HasDiagnostic(diagnostics, "SG3011"),
            "A duplicate migration file for the same version must be reported."
        );
    }

    // A migration file above the current version is dead data - usually a version that was
    // rolled back without deleting the file. The file at the current version is the schema
    // record the migration tool maintains and must not be flagged.
    [Fact]
    public void StaleMigrationFile_ReportsDiagnostic()
    {
        var currentJson = MigrationJson.Replace("\"version\": 0", "\"version\": 1");
        var staleJson = MigrationJson.Replace("\"version\": 0", "\"version\": 2");

        var (diagnostics, _) = SourceGeneratorTestHelper.RunGenerator(
            Source,
            additionalTexts:
            [
                ("Server.TestContent.MigratingItem.v0.json", MigrationJson),
                ("Server.TestContent.MigratingItem.v1.json", currentJson),
                ("Server.TestContent.MigratingItem.v2.json", staleJson)
            ]
        );

        Assert.Equal(
            1,
            diagnostics.Count(d => d.Id == "SG3012")
        );
    }
}
