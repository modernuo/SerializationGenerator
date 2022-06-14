/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2022 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: EntityJsonGenerator.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ModernUO.Serialization.Generator;

[Generator]
public class EntitySerializationGenerator : IIncrementalGenerator
{
    private string _migrationPath;

    public EntitySerializationGenerator() : this(null)
    {
    }

    public EntitySerializationGenerator(string? migrationPath = null) => _migrationPath = migrationPath;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather all classes with [ModernUO.Serialization.Serializable] attribute
        var serializableClasses = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                (node, token) => node.IsAttributedSyntaxNode<ClassDeclarationSyntax>("SerializationGenerator", token),
                GetSerializableClassAndProperties
            );

        // Gather all migration JSON files and organize them by file name (namespace/class) and version.
        var migrationFiles = context
            .AdditionalTextsProvider
            .Collect()
            .Select(ToMigrationFileSet);

        // Combine the classes, migrations, and fields into a single set
        var classesWithMigrations = serializableClasses
            .Combine(migrationFiles)
            .Select(TransformToClassMigrationPairs)
            .Combine(context.CompilationProvider);

        // Generate source code
        context.RegisterSourceOutput(classesWithMigrations, ExecuteIncremental);
    }

    private static SerializableClassRecord GetSerializableClassAndProperties(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var compilation = ctx.SemanticModel.Compilation;

        var node = (ClassDeclarationSyntax)ctx.Node.Parent!.Parent!;
        var fieldsAndProperties = ImmutableArray.CreateBuilder<(ISymbol, AttributeData)>();
        ISymbol? dirtyTrackingEntity = null;
        foreach (var m in node.Members)
        {
            token.ThrowIfCancellationRequested();

            if (m is PropertyDeclarationSyntax propertyNode)
            {
                if (ctx.SemanticModel.GetDeclaredSymbol(propertyNode) is IPropertySymbol propertySymbol)
                {
                    if (propertySymbol.TryGetDirtyTrackingEntityField(compilation))
                    {
                        dirtyTrackingEntity = propertySymbol;
                    }

                    if (propertySymbol.TryGetSerializableField(compilation, out var attributeData))
                    {
                        fieldsAndProperties.Add((propertySymbol, attributeData));
                    }
                }
            }
            else if (m is FieldDeclarationSyntax fieldNode)
            {
                foreach (var variable in fieldNode.Declaration.Variables)
                {
                    token.ThrowIfCancellationRequested();

                    if (ctx.SemanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol)
                    {
                        if (fieldSymbol.TryGetDirtyTrackingEntityField(compilation))
                        {
                            dirtyTrackingEntity = fieldSymbol;
                        }

                        if (fieldSymbol.TryGetSerializableField(compilation, out var attributeData))
                        {
                            fieldsAndProperties.Add((fieldSymbol, attributeData));
                        }
                    }
                }
            }
        }

        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        classSymbol.TryGetSerializable(compilation, out var serializationAttribute);
        return new SerializableClassRecord(
            classSymbol,
            serializationAttribute,
            fieldsAndProperties.ToImmutable(),
            dirtyTrackingEntity,
            ImmutableDictionary<int, AdditionalText>.Empty
        );
    }

    private static SerializableClassRecord TransformToClassMigrationPairs(
        (SerializableClassRecord, ImmutableDictionary<string, Dictionary<int, AdditionalText>>) pair,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var (classRecord, additionalTexts) = pair;

        var namespaceName = classRecord.classSymbol.ContainingNamespace.ToDisplayString();
        var className = $"{namespaceName}.{classRecord.classSymbol.Name}";

        if (additionalTexts.TryGetValue(className, out var migs))
        {
            return classRecord with
            {
                migrations = migs.ToImmutableDictionary()
            };
        }

        return classRecord;
    }

    private static ImmutableDictionary<string, Dictionary<int, AdditionalText>> ToMigrationFileSet(
        ImmutableArray<AdditionalText> additionalTexts,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var builder = ImmutableDictionary.CreateBuilder<string, Dictionary<int, AdditionalText>>();

        foreach (var additionalText in additionalTexts)
        {
            token.ThrowIfCancellationRequested();

            var path = additionalText.Path;
            var fileName = Path.GetFileName(path);
            if (!SerializableMigrationSchema.MatchMigrationFilename(fileName, out var className, out var version))
            {
                continue;
            }

            if (!builder.TryGetValue(className, out var classMigrationSet))
            {
                builder[className] = classMigrationSet = new Dictionary<int, AdditionalText>();
            }

            classMigrationSet[version] = additionalText;
        }

        return builder.ToImmutable();
    }

    private void ExecuteIncremental(
        SourceProductionContext context,
        (SerializableClassRecord classRecord, Compilation compilation) combined
    )
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var (classRecord, compilation) = combined;

        var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions();

        string classSource = compilation.GenerateSerializationPartialClass(
            classRecord.classSymbol,
            classRecord.serializationAttribute,
            jsonOptions,
            classRecord.migrations,
            classRecord.fieldsAndProperties,
            classRecord.dirtyTrackingEntity,
            _migrationPath,
            context.CancellationToken
        );

        if (classSource != null)
        {
            context.AddSource(
                $"{classRecord.classSymbol.ToDisplayString()}.Serialization.cs",
                SourceText.From(classSource, Encoding.UTF8)
            );
        }
    }
}
