/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EntitySerializationGenerator.cs                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
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

    public Dictionary<string, SerializableMetadata> Migrations { get; } = [];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var currentCulture = CultureInfo.DefaultThreadCurrentCulture;

        // Gather all classes with [ModernUO.Serialization.SerializationGenerator] attribute
        var serializableClasses = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsSerializationGeneratorSyntaxNode,
                GetSerializableClassAndProperties
            )
            .Where(t => t != null && (t.Value.Item1 != null || t.Value.Item2 != null))
            .WithTrackingName("serializableClasses");

        // Gather all migration JSON files and organize them by file name (namespace/class) and version.
        var migrationFiles = context
            .AdditionalTextsProvider
            .Collect()
            .Select(ToMigrationFileSet)
            .WithTrackingName("migrations");

        // Combine the classes, migrations, and fields into a single set
        var classesWithMigrations = serializableClasses
            .Combine(migrationFiles)
            .Select(TransformToClassMigrationPairs)
            .Combine(context.CompilationProvider);

        // Generate source code
        context.RegisterSourceOutput(classesWithMigrations, ExecuteIncremental);
        CultureInfo.DefaultThreadCurrentCulture = currentCulture;
    }

    public static bool IsSerializationGeneratorSyntaxNode(SyntaxNode node, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var name = (node as AttributeSyntax)?.Name.ExtractName();
        return name is "SerializationGenerator" or "SerializationGeneratorAttribute";
    }

    private static (SerializableClassRecord, Diagnostic[])? GetSerializableClassAndProperties(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var compilation = ctx.SemanticModel.Compilation;

        var syntaxNode = (AttributeSyntax)ctx.Node;

        if (syntaxNode.Parent?.Parent is not ClassDeclarationSyntax classNode)
        {
            return null;
        }

        if (!classNode.IsPartial())
        {
            var className = compilation
                .GetSemanticModel(classNode.SyntaxTree)
                .GetDeclaredSymbol(classNode)?
                .Name;

            var diagnostic = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3001, className);
            return (null, [diagnostic]);
        }

        var node = (ClassDeclarationSyntax)ctx.Node.Parent!.Parent!;
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;

        // This happens when there is no using import
        if (!classSymbol.TryGetSerializable(compilation, out var serializationAttribute))
        {
            var className = compilation
                .GetSemanticModel(classNode.SyntaxTree)
                .GetDeclaredSymbol(classNode)?
                .Name;

            var diagnostic = classNode.GenerateDiagnostic(DiagnosticDescriptors.SG3002, className);

            return (null, [diagnostic]);
        }

        var fields = ImmutableArray.CreateBuilder<(ISymbol, AttributeData)>();
        var properties = ImmutableArray.CreateBuilder<(ISymbol, AttributeData)>();
        var saveFlagMethods = ImmutableArray.CreateBuilder<(ISymbol, AttributeData)>();
        var defaultValueMethods = ImmutableArray.CreateBuilder<(ISymbol, AttributeData)>();
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
                    else if (propertySymbol.TryGetSerializableProperty(compilation, out var attributeData))
                    {
                        properties.Add((propertySymbol, attributeData));
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
                        else if (fieldSymbol.TryGetSerializableField(compilation, out var attributeData))
                        {
                            fields.Add((fieldSymbol, attributeData));
                        }
                    }
                }
            }
            else if (m is MethodDeclarationSyntax methodNode)
            {
                if (ctx.SemanticModel.GetDeclaredSymbol(methodNode) is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.TryGetSerializableFieldSaveFlagMethod(compilation, out var attributeData))
                    {
                        saveFlagMethods.Add((methodSymbol, attributeData));
                    }
                    else if (methodSymbol.TryGetSerializableFieldDefaultMethod(compilation, out attributeData))
                    {
                        defaultValueMethods.Add((methodSymbol, attributeData));
                    }
                }
            }
        }

        var record = new SerializableClassRecord(
            classNode,
            classSymbol,
            serializationAttribute,
            fields.ToImmutable(),
            properties.ToImmutable(),
            saveFlagMethods.ToImmutable(),
            defaultValueMethods.ToImmutable(),
            dirtyTrackingEntity,
            ImmutableDictionary<int, AdditionalText>.Empty
        );

        return (record, []);
    }

    private static (SerializableClassRecord, Diagnostic[]) TransformToClassMigrationPairs(
        ((SerializableClassRecord, Diagnostic[])?, ImmutableDictionary<string, Dictionary<int, AdditionalText>>) pair,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var (recordPair, additionalTexts) = pair;
        if (!recordPair.HasValue)
        {
            return (null, []);
        }

        var (classRecord, diags) = recordPair.Value;

        if (classRecord != null)
        {
            var namespaceName = classRecord.ClassSymbol.ContainingNamespace.ToDisplayString();
            var className = $"{namespaceName}.{classRecord.ClassSymbol.Name}";

            if (additionalTexts.TryGetValue(className, out var migs))
            {
                classRecord = classRecord with
                {
                    Migrations = migs.ToImmutableDictionary()
                };
            }
        }

        return (classRecord, diags);
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
        ((SerializableClassRecord, Diagnostic[]), Compilation) combined
    )
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var ((classRecord, prereqFailures), compilation) = combined;

        if (prereqFailures.Length > 0)
        {
            for (var i = 0; i < prereqFailures.Length; i++)
            {
                context.ReportDiagnostic(prereqFailures[i]);
            }

            return;
        }

        if (classRecord != null)
        {
            var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions();

            try
            {
                var (classSource, migration, diags) = compilation.GenerateSerializationPartialClass(
                    classRecord,
                    jsonOptions,
                    generateMigrations,
                    context.CancellationToken
                );

                if (classSource != null)
                {
                    context.AddSource(
                        $"{classRecord.ClassSymbol.ToDisplayString()}.Serialization.g.cs",
                        SourceText.From(classSource, Encoding.UTF8)
                    );

                    if (migration != null)
                    {
                        Migrations[migration.Type] = migration;
                    }
                }
                else
                {
                    for (var i = 0; i < diags.Length; i++)
                    {
                        context.ReportDiagnostic(diags[i]);
                    }
                }
            }
            catch (Exception e)
            {
                var descriptor = DiagnosticDescriptors.GeneratorCrashedDiagnostic(e);
                var diagnostic = classRecord.ClassNode.GenerateDiagnostic(
                    descriptor,
                    e.GetType(),
                    classRecord.ClassSymbol.Name,
                    e.Message
                );

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
