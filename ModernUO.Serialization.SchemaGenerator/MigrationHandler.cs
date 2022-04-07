using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SerializableMigration;

namespace SerializationSchemaGenerator;

public static class MigrationHandler
{
    public static ImmutableArray<AdditionalText> GetMigrations(string migrationPath)
    {
        var migrations = ImmutableArray.CreateBuilder<AdditionalText>();
        var migrationFiles = Directory.GetFiles(migrationPath, "*.v*.json");

        foreach (var file in migrationFiles)
        {
            var sourceText = SourceText.From(File.ReadAllText(file), Encoding.UTF8);
            migrations.Add(new AdditionalTextSchemaFile(file, sourceText));
        }

        return migrations.ToImmutable();
    }
}
