/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EntitySerializationGenerator.cs                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ModernUO.Serialization.Generator;

[Generator]
public class EntitySerializationGenerator(bool generateMigrations = false) : IIncrementalGenerator
{
    public EntitySerializationGenerator() : this(false)
    {
    }

    // Populated concurrently: RegisterSourceOutput callbacks can run in parallel.
    public ConcurrentDictionary<string, SerializableMetadata> Migrations { get; } = [];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Fully resolved, value-equatable models. The transform re-runs when the compilation
        // changes, but an edit that does not affect the serialization surface produces an
        // equal model and everything downstream stays cached.
        var serializableClasses = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                SymbolMetadata.SERIALIZABLE_ATTRIBUTE,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, token) => SerializableEntityGeneration.BuildSerializationModel(ctx, token)
            )
            .WithTrackingName("serializableClasses");

        // Each migration file parses once and re-parses only when its content changes.
        var migrationFiles = context
            .AdditionalTextsProvider
            .Where(static text => SerializableMigrationSchema.MatchMigrationFilename(
                Path.GetFileName(text.Path), out _, out _)
            )
            .Select(static (text, token) => ParseMigrationFile(text, token))
            .WithTrackingName("migrationFiles");

        // Value-type facts need the compilation; this recomputes cheaply per compilation and
        // produces equal output while the answers are unchanged, keeping downstream cached.
        var augmentedMigrations = migrationFiles
            .Combine(context.CompilationProvider)
            .Select(static (pair, token) => AugmentMigrationFile(pair.Left, pair.Right, token))
            .WithTrackingName("augmentedMigrations");

        var migrationMap = augmentedMigrations
            .Collect()
            .Select(static (files, token) => MigrationFileMap.Create(files, token))
            .WithTrackingName("migrationMap");

        var classesWithMigrations = serializableClasses
            .Combine(migrationMap)
            .Select(static (pair, token) => AttachMigrations(pair.Left, pair.Right, token))
            .WithTrackingName("classesWithMigrations");

        context.RegisterSourceOutput(classesWithMigrations, ExecuteIncremental);
    }

    private static MigrationFileModel ParseMigrationFile(AdditionalText text, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        SerializableMigrationSchema.MatchMigrationFilename(
            Path.GetFileName(text.Path), out var className, out var version
        );

        var sourceText = text.GetText(token);
        if (sourceText == null)
        {
            return new MigrationFileModel(className, version, text.Path, null, "file could not be read");
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<SerializableMetadata>(
                sourceText.ToString(),
                SerializableMigrationSchema.GetJsonSerializerOptions()
            );

            return new MigrationFileModel(className, version, text.Path, metadata, null);
        }
        catch (Exception e)
        {
            return new MigrationFileModel(className, version, text.Path, null, e.Message);
        }
    }

    private static MigrationFileModel AugmentMigrationFile(
        MigrationFileModel file,
        Compilation compilation,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        if (file.Metadata?.Properties is not { } properties)
        {
            return file;
        }

        var builder = ImmutableArray.CreateBuilder<SerializableProperty>(properties.Length);
        foreach (var property in properties)
        {
            // The fact only affects the nullable suffix on save-flagged content struct fields.
            builder.Add(
                property.UsesSaveFlag != true
                    ? property
                    : property with
                    {
                        TypeIsValueType = compilation.GetCachedTypeByMetadataName(property.Type)?.IsValueType == true
                    }
            );
        }

        return file with { Metadata = file.Metadata with { Properties = builder.MoveToImmutable() } };
    }

    private static FinalModel AttachMigrations(
        SerializationModelResult result,
        MigrationFileMap map,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var model = result.Model;
        if (model == null)
        {
            return new FinalModel(result, EquatableArray<SerializableMetadata>.Empty, EquatableArray<DiagnosticInfo>.Empty);
        }

        var files = map.GetFiles(model.ArityName);
        if (files.Count == 0)
        {
            return new FinalModel(result, EquatableArray<SerializableMetadata>.Empty, EquatableArray<DiagnosticInfo>.Empty);
        }

        var location = model.Location;
        var diagnostics = new List<DiagnosticInfo>();
        var byVersion = new Dictionary<int, MigrationFileModel>();

        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();

            if (file.Error != null)
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        "SG3013", location.FilePath, location.Span, location.LineSpan,
                        new[] { file.FilePath, file.Error }.ToEquatableArray()
                    )
                );
                continue;
            }

            // Only use the first migration file for each version; the rest are reported (SG3011).
            if (byVersion.ContainsKey(file.Version))
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        "SG3011", location.FilePath, location.Span, location.LineSpan,
                        new[] { file.FilePath }.ToEquatableArray()
                    )
                );
                continue;
            }

            byVersion[file.Version] = file;

            // The file at the current version is the schema record the migration tool
            // maintains; anything beyond it is left over from a rolled-back version bump.
            if (file.Version > model.Version)
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        "SG3012", location.FilePath, location.Span, location.LineSpan,
                        new[] { file.Version.ToString(), model.Version.ToString() }.ToEquatableArray()
                    )
                );
            }
        }

        var migrations = new List<SerializableMetadata>();
        for (var i = 0; i < model.Version; i++)
        {
            if (byVersion.TryGetValue(i, out var file) && file.Metadata != null)
            {
                migrations.Add(file.Metadata);
            }
        }

        return new FinalModel(result, migrations.ToEquatableArray(), diagnostics.ToEquatableArray());
    }

    private void ExecuteIncremental(SourceProductionContext context, FinalModel finalModel)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        foreach (var diagnostic in finalModel.Result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        foreach (var diagnostic in finalModel.MigrationDiagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        var model = finalModel.Result.Model;
        if (model == null)
        {
            return;
        }

        try
        {
            var (classSource, migration) = SerializableEntityGeneration.GenerateFromModel(
                model,
                finalModel.Migrations.AsImmutableArray(),
                generateMigrations,
                context.CancellationToken
            );

            // Use arity notation for generic types to avoid invalid characters in filename
            context.AddSource(
                $"{model.ArityName}.Serialization.g.cs",
                SourceText.From(classSource, Encoding.UTF8)
            );

            if (migration != null)
            {
                Migrations[migration.Type] = migration;
            }
        }
        catch (Exception e)
        {
            var descriptor = DiagnosticDescriptors.GeneratorCrashedDiagnostic(e);
            var diagnostic = Diagnostic.Create(
                descriptor,
                model.Location.ToLocation(),
                e.GetType(),
                model.ClassName,
                e.Message,
                e.StackTrace
            );

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Migration files grouped by class name, with deep value equality so an unchanged set
    /// keeps downstream nodes cached.
    /// </summary>
    public sealed class MigrationFileMap : IEquatable<MigrationFileMap>
    {
        private static readonly List<MigrationFileModel> _empty = [];

        private readonly Dictionary<string, List<MigrationFileModel>> _files;

        private MigrationFileMap(Dictionary<string, List<MigrationFileModel>> files) => _files = files;

        public static MigrationFileMap Create(ImmutableArray<MigrationFileModel> files, CancellationToken token)
        {
            var map = new Dictionary<string, List<MigrationFileModel>>();

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                if (!map.TryGetValue(file.ClassName, out var list))
                {
                    map[file.ClassName] = list = [];
                }

                list.Add(file);
            }

            return new MigrationFileMap(map);
        }

        public List<MigrationFileModel> GetFiles(string className) =>
            _files.TryGetValue(className, out var list) ? list : _empty;

        public bool Equals(MigrationFileMap other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (_files.Count != other._files.Count)
            {
                return false;
            }

            foreach (var kvp in _files)
            {
                if (!other._files.TryGetValue(kvp.Key, out var otherList) || kvp.Value.Count != otherList.Count)
                {
                    return false;
                }

                for (var i = 0; i < kvp.Value.Count; i++)
                {
                    if (!kvp.Value[i].Equals(otherList[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is MigrationFileMap other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = _files.Count;
                foreach (var kvp in _files)
                {
                    hash ^= kvp.Key.GetHashCode() * 31 + kvp.Value.Count;
                }

                return hash;
            }
        }
    }
}
