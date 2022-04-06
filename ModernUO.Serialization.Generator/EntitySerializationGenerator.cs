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
using SerializableMigration;

namespace SerializationGenerator;

[Generator]
public class EntitySerializationGenerator : IIncrementalGenerator
{
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
            .Collect();

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
            .Select(
                (tuple, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var ((classSymbol, attributeData, additionalTexts), fields) = tuple;
                    return (classSymbol, attributeData, additionalTexts, fields);
                }
            );

        // Generate source code
        context.RegisterSourceOutput(classesWithMigrationsAndFields.Combine(context.CompilationProvider), ExecuteIncremental);
    }

    private static (ISymbol, AttributeData)? GetSerializablePropertyDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var propertyNode = (PropertyDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(propertyNode) is not IPropertySymbol propertySymbol)
        {
            return null;
        }

        if (!propertySymbol.TryGetSerializableField(ctx.SemanticModel.Compilation, out var attributeData))
        {
            return null;
        }

        return (propertySymbol, attributeData);
    }

    private static ImmutableArray<(ISymbol, AttributeData)> GetSerializableFieldDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var fields = new List<(ISymbol, AttributeData)>();
        var fieldNode = (FieldDeclarationSyntax)ctx.Node;
        foreach (var variable in fieldNode.Declaration.Variables)
        {
            token.ThrowIfCancellationRequested();
            if (ctx.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            if (fieldSymbol.TryGetSerializableField(ctx.SemanticModel.Compilation, out var attributeData))
            {
                fields.Add((fieldSymbol, attributeData));
            }
            else if (fieldSymbol.TryGetSerializableParentField(ctx.SemanticModel.Compilation, out attributeData))
            {
                fields.Add((fieldSymbol, attributeData));
            }
        }

        return fields.Count == 0 ? ImmutableArray<(ISymbol, AttributeData)>.Empty : fields.ToImmutableArray();
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

        var dictionary = new Dictionary<string, Dictionary<int, AdditionalText>>();

        foreach (var additionalText in additionalTexts)
        {
            token.ThrowIfCancellationRequested();

            var path = additionalText.Path;
            var fileName = Path.GetFileName(path);
            if (!SerializableMigrationSchema.MatchMigrationFilename(fileName, out var className, out var version))
            {
                continue;
            }

            var classMigrationSet = dictionary[className] ??= new Dictionary<int, AdditionalText>();
            classMigrationSet[version] = additionalText;
        }

        return dictionary.ToImmutableDictionary();
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
        ((INamedTypeSymbol classSymbol, AttributeData serializableAttribute, ImmutableDictionary<int, AdditionalText> migrations,
            ImmutableArray<(ISymbol, AttributeData)> fieldsAndProperties) classData,
        Compilation compilation) combined
    )
    {
        var ((classSymbol, serializableAttribute, migrations, fieldsAndProperties), compilation) = combined;
        context.CancellationToken.ThrowIfCancellationRequested();
        var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions();

        string classSource = compilation.GenerateSerializationPartialClass(
            classSymbol,
            serializableAttribute,
            jsonOptions,
            migrations,
            fieldsAndProperties,
            context.CancellationToken
        );

        if (classSource != null)
        {
            context.AddSource($"{classSymbol.ToDisplayString()}.Serialization.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }
}
