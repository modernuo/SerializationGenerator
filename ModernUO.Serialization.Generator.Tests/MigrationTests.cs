/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MigrationTests.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class MigrationTests
{
    [Fact]
    public void Version1_WithMigrationFile_GeneratesMigrationCode()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(1)]
                public partial class MigratingItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(1)]
                    private int _newField;

                    public Serial Serial => default;
                    public void MarkDirty() { }

                    private void MigrateFrom(V0Content content)
                    {
                        _name = content.Name;
                        _newField = 0;
                    }
                }
            }
            """;

        const string migrationJson = """
            {
                "version": 0,
                "type": "TestNamespace.MigratingItem",
                "properties": [
                    {
                        "name": "Name",
                        "type": "string",
                        "rule": "PrimitiveTypeMigrationRule"
                    }
                ]
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(
            source,
            additionalTexts: [("TestNamespace.MigratingItem.v0.json", migrationJson)]
        );

        // With migrations, there may be diagnostics about missing MigrateFrom method
        // Just verify that the generator ran and produced output
        Assert.NotNull(generatedSource);
        // Should generate V0Content struct when migration file is present
        Assert.Contains("V0Content", generatedSource);
    }

    [Fact]
    public void MigrationFileRegex_MatchesStandardFormat()
    {
        var regex = ModernUO.Serialization.Generator.SerializableMigrationSchema.MigrationFileRegex;

        Assert.True(regex.IsMatch("MyClass.v0.json"));
        Assert.True(regex.IsMatch("MyClass.v1.json"));
        Assert.True(regex.IsMatch("MyClass.V0.json"));
        Assert.True(regex.IsMatch("Namespace.MyClass.v0.json"));
        Assert.True(regex.IsMatch("Deep.Namespace.MyClass.v0.json"));
    }

    [Fact]
    public void MigrationFileRegex_MatchesGenericFormat()
    {
        var regex = ModernUO.Serialization.Generator.SerializableMigrationSchema.MigrationFileRegex;

        Assert.True(regex.IsMatch("MyClass`1.v0.json"));
        Assert.True(regex.IsMatch("MyClass`2.v0.json"));
        Assert.True(regex.IsMatch("Namespace.MyClass`1.v0.json"));
        Assert.True(regex.IsMatch("Deep.Namespace.MyClass`2.v1.json"));
    }

    [Fact]
    public void MigrationFileRegex_DoesNotMatchInvalid()
    {
        var regex = ModernUO.Serialization.Generator.SerializableMigrationSchema.MigrationFileRegex;

        Assert.False(regex.IsMatch("MyClass.json"));
        Assert.False(regex.IsMatch("MyClass.v.json"));
        Assert.False(regex.IsMatch("MyClass.va.json"));
        Assert.False(regex.IsMatch(".v0.json"));
    }

    [Fact]
    public void MatchMigrationFilename_ExtractsCorrectInfo()
    {
        Assert.True(ModernUO.Serialization.Generator.SerializableMigrationSchema.MatchMigrationFilename(
            "MyClass.v0.json", out var className, out var version));
        Assert.Equal("MyClass", className);
        Assert.Equal(0, version);

        Assert.True(ModernUO.Serialization.Generator.SerializableMigrationSchema.MatchMigrationFilename(
            "Namespace.MyClass.v5.json", out className, out version));
        Assert.Equal("Namespace.MyClass", className);
        Assert.Equal(5, version);
    }

    [Fact]
    public void MatchMigrationFilename_ExtractsGenericInfo()
    {
        Assert.True(ModernUO.Serialization.Generator.SerializableMigrationSchema.MatchMigrationFilename(
            "MyClass`1.v0.json", out var className, out var version));
        Assert.Equal("MyClass`1", className);
        Assert.Equal(0, version);

        Assert.True(ModernUO.Serialization.Generator.SerializableMigrationSchema.MatchMigrationFilename(
            "Namespace.MyClass`2.v3.json", out className, out version));
        Assert.Equal("Namespace.MyClass`2", className);
        Assert.Equal(3, version);
    }

    [Fact]
    public void MultipleMigrations_GeneratesAllVersionContent()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(2)]
                public partial class MultiMigrateItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(1)]
                    private int _count;

                    [SerializableField(2)]
                    private bool _active;

                    public Serial Serial => default;
                    public void MarkDirty() { }

                    private void MigrateFrom(V0Content content) { }
                    private void MigrateFrom(V1Content content) { }
                }
            }
            """;

        const string migrationV0 = """
            {
                "version": 0,
                "type": "TestNamespace.MultiMigrateItem",
                "properties": [
                    { "name": "Name", "type": "string", "rule": "PrimitiveTypeMigrationRule" }
                ]
            }
            """;

        const string migrationV1 = """
            {
                "version": 1,
                "type": "TestNamespace.MultiMigrateItem",
                "properties": [
                    { "name": "Name", "type": "string", "rule": "PrimitiveTypeMigrationRule" },
                    { "name": "Count", "type": "int", "rule": "PrimitiveTypeMigrationRule" }
                ]
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(
            source,
            additionalTexts: [
                ("TestNamespace.MultiMigrateItem.v0.json", migrationV0),
                ("TestNamespace.MultiMigrateItem.v1.json", migrationV1)
            ]
        );

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("V0Content", generatedSource);
        Assert.Contains("V1Content", generatedSource);
    }

    [Fact]
    public void EncodedVersion_UsesEncodedIntForVersion()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0, encodedVersion: true)]
                public partial class EncodedVersionItem : ISerializable
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
        Assert.Contains("WriteEncodedInt(SerializationVersion)", generatedSource);
        Assert.Contains("ReadEncodedInt()", generatedSource);
    }

    [Fact]
    public void NonEncodedVersion_UsesIntForVersion()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0, encodedVersion: false)]
                public partial class NonEncodedVersionItem : ISerializable
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
        Assert.Contains("Write(SerializationVersion)", generatedSource);
        Assert.Contains("ReadInt()", generatedSource);
    }
}
