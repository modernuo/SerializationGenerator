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
                Helpers.IsSyntaxNode<ClassDeclarationSyntax>,
                GetSerializableClassDeclaration
            )
            .RemoveNulls();

        // Used for dirty tracking embedded classes
        var dirtyTrackingFields = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                Helpers.IsSyntaxNode<FieldDeclarationSyntax, PropertyDeclarationSyntax>,
                GetDirtyTrackingEntityFieldDeclaration
            )
            .Flatten()
            .ToImmutableDictionary();

        var serializableFields = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                Helpers.IsSyntaxNode<FieldDeclarationSyntax>,
                GetSerializableFieldDeclaration
            )
            .Flatten();

        var serializableProperties = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                Helpers.IsSyntaxNode<PropertyDeclarationSyntax>,
                GetSerializablePropertyDeclaration
            )
            .RemoveNulls();

        var serializableFieldsAndProperties = serializableFields
            .Merge(serializableProperties)
            .Collect()
            .Select(ToFieldsAndPropertiesSet);

        // Gather all migration JSON files and organize them by file name (namespace/class) and version.
        var migrationFiles = context
            .AdditionalTextsProvider
            .Collect()
            .Select(ToMigrationFileSet);

        // Combine the classes, migrations, and fields into a single set
        var classesWithMigrationsAndFields = serializableClasses
            .Combine(migrationFiles)
            .Select(TransformToClassMigrationPairs)
            .Combine(serializableFieldsAndProperties)
            .Combine(dirtyTrackingFields)
            .Select(
                (tuple, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var (((classSymbol, attributeData, additionalTexts), fieldsSet), dirtyTrackingEntitySet) = tuple;

                    ImmutableArray<(ISymbol, AttributeData)> fields;
                    if (fieldsSet.TryGetValue(classSymbol, out var fieldsList))
                    {
                        fields = fieldsList.ToImmutableArray();
                    }
                    else
                    {
                        fields = ImmutableArray<(ISymbol, AttributeData)>.Empty;
                    }

                    dirtyTrackingEntitySet.TryGetValue(classSymbol, out var dirtyTrackingEntityField);
                    return (classSymbol, attributeData, additionalTexts, fields, dirtyTrackingEntityField);
                }
            );

        // Generate source code
        context.RegisterSourceOutput(classesWithMigrationsAndFields.Combine(context.CompilationProvider), ExecuteIncremental);
    }

    private static ImmutableArray<(INamedTypeSymbol, ISymbol)> GetDirtyTrackingEntityFieldDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var fields = ImmutableArray.CreateBuilder<(INamedTypeSymbol, ISymbol)>();
        if (ctx.Node is PropertyDeclarationSyntax propertyNode)
        {
            if (ctx.SemanticModel.GetDeclaredSymbol(propertyNode) is IPropertySymbol propertySymbol &&
                propertyNode.Parent is ClassDeclarationSyntax classNode &&
                ctx.SemanticModel.GetDeclaredSymbol(classNode) is INamedTypeSymbol classSymbol &&
                classSymbol.TryGetSerializable(ctx.SemanticModel.Compilation, out _) &&
                propertySymbol.TryGetDirtyTrackingEntityField(ctx.SemanticModel.Compilation, out _))
            {
                fields.Add((classSymbol, propertySymbol));
            }
        }
        else if (ctx.Node is FieldDeclarationSyntax fieldNode)
        {
            foreach (var variable in fieldNode.Declaration.Variables)
            {
                token.ThrowIfCancellationRequested();

                if (ctx.SemanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol &&
                    fieldNode.Parent is ClassDeclarationSyntax classNode &&
                    ctx.SemanticModel.GetDeclaredSymbol(classNode) is INamedTypeSymbol classSymbol &&
                    classSymbol.TryGetSerializable(ctx.SemanticModel.Compilation, out _) &&
                    fieldSymbol.TryGetDirtyTrackingEntityField(ctx.SemanticModel.Compilation, out _))
                {
                    fields.Add((classSymbol, fieldSymbol));
                }
            }
        }

        return fields.ToImmutable();
    }

    private static (INamedTypeSymbol, ISymbol, AttributeData)? GetSerializablePropertyDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var propertyNode = (PropertyDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(propertyNode) is not IPropertySymbol propertySymbol ||
            !propertySymbol.TryGetSerializableField(ctx.SemanticModel.Compilation, out var attributeData) ||
            propertyNode.Parent is not ClassDeclarationSyntax classNode ||
            ctx.SemanticModel.GetDeclaredSymbol(classNode) is not INamedTypeSymbol classSymbol ||
            !classSymbol.TryGetSerializable(ctx.SemanticModel.Compilation, out _))
        {
            return null;
        }

        return (classSymbol, propertySymbol, attributeData);
    }

    private static ImmutableArray<(INamedTypeSymbol, ISymbol, AttributeData)> GetSerializableFieldDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var fieldNode = (FieldDeclarationSyntax)ctx.Node;
        var fields = new List<(INamedTypeSymbol, ISymbol, AttributeData)>();
        foreach (var variable in fieldNode.Declaration.Variables)
        {
            token.ThrowIfCancellationRequested();
            if (ctx.SemanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol &&
                fieldNode.Parent is ClassDeclarationSyntax classNode &&
                ctx.SemanticModel.GetDeclaredSymbol(classNode) is INamedTypeSymbol classSymbol &&
                classSymbol.TryGetSerializable(ctx.SemanticModel.Compilation, out _) &&
                fieldSymbol.TryGetSerializableField(ctx.SemanticModel.Compilation, out var attributeData))
            {
                fields.Add((classSymbol, fieldSymbol, attributeData));
            }
        }

        return fields.Count == 0 ? ImmutableArray<(INamedTypeSymbol, ISymbol, AttributeData)>.Empty : fields.ToImmutableArray();
    }

    private static
        (INamedTypeSymbol, AttributeData, ImmutableDictionary<int, AdditionalText>)
        TransformToClassMigrationPairs(
            ((INamedTypeSymbol, AttributeData), ImmutableDictionary<string, Dictionary<int, AdditionalText>>) pair,
            CancellationToken token
        )
    {
        token.ThrowIfCancellationRequested();

        var ((classSymbol, attributeData), additionalTexts) = pair;

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = $"{namespaceName}.{classSymbol.Name}";
        additionalTexts.TryGetValue(className, out var migrations);

        return (classSymbol, attributeData, migrations?.ToImmutableDictionary() ?? ImmutableDictionary<int, AdditionalText>.Empty);
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

    private static ImmutableDictionary<INamedTypeSymbol, List<(ISymbol, AttributeData)>> ToFieldsAndPropertiesSet(
        ImmutableArray<(INamedTypeSymbol, ISymbol, AttributeData)> fieldsAndProperties, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var builder = ImmutableDictionary.CreateBuilder<INamedTypeSymbol, List<(ISymbol, AttributeData)>>(SymbolEqualityComparer.Default);

        for (var i = 0; i < fieldsAndProperties.Length; i++)
        {
            var (classSymbol, fieldSymbol, fieldAttr) = fieldsAndProperties[i];
            if (!builder.TryGetValue(classSymbol, out var list))
            {
                builder[classSymbol] = list = new List<(ISymbol, AttributeData)>();

            }

            list.Add((fieldSymbol, fieldAttr));
        }

        return builder.ToImmutable();
    }

    private static (INamedTypeSymbol, AttributeData)? GetSerializableClassDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();
        var classNode = (ClassDeclarationSyntax)ctx.Node;
        var classSymbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(classNode);

        if (classSymbol.TryGetSerializable(ctx.SemanticModel.Compilation, out var attributeData))
        {
            return (classSymbol, attributeData);
        }

        return null;
    }

    private void ExecuteIncremental(
        SourceProductionContext context,
        ((INamedTypeSymbol classSymbol, AttributeData serializableAttribute, ImmutableDictionary<int, AdditionalText>
            migrations, ImmutableArray<(ISymbol, AttributeData)> fieldsAndProperties, ISymbol? dirtyTrackingEntityField)
            classData, Compilation compilation) combined
    )
    {
        var ((classSymbol, serializableAttribute, migrations, fieldsAndProperties, dirtyTrackingEntityField), compilation) = combined;

        context.CancellationToken.ThrowIfCancellationRequested();
        var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions();

        string classSource = compilation.GenerateSerializationPartialClass(
            classSymbol,
            serializableAttribute,
            jsonOptions,
            migrations,
            fieldsAndProperties,
            dirtyTrackingEntityField,
            _migrationPath,
            context.CancellationToken
        );

        if (classSource != null)
        {
            context.AddSource(
                $"{classSymbol.ToDisplayString()}.Serialization.cs",
                SourceText.From(classSource, Encoding.UTF8)
            );
        }
    }
}
