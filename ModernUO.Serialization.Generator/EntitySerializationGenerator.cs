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

        // Gather all migration JSON files and organize them by file name (namespace/class) and version.
        var migrationFiles = context
            .AdditionalTextsProvider
            .Collect()
            .Select(ToMigrationFileSet);

        // Combine the classes, migrations, and fields into a single set
        var classesWithMigrationsAndFields = serializableClasses
            .Combine(migrationFiles)
            .Select(TransformToClassMigrationPairs)
            .Combine(serializableFields.Merge(serializableProperties).Collect())
            .Select(
                (tuple, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var ((classSymbol, attributeData, additionalTexts), fields) = tuple;
                    return (classSymbol, attributeData, additionalTexts, fields);
                }
            );

        // Generate source code
        context.RegisterSourceOutput(classesWithMigrationsAndFields, ExecuteIncremental);
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
        }

        return fields.Count == 0 ? ImmutableArray<(ISymbol, AttributeData)>.Empty : fields.ToImmutableArray();
    }

    private static
        (INamedTypeSymbol, AttributeData, Dictionary<int, AdditionalText>?)
        TransformToClassMigrationPairs(
            ((INamedTypeSymbol, AttributeData), Dictionary<string, Dictionary<int, AdditionalText>>) pair,
            CancellationToken token
        )
    {
        token.ThrowIfCancellationRequested();

        var ((classSymbol, attributeData), additionalTexts) = pair;

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = $"{namespaceName}.{classSymbol.Name}";
        additionalTexts.TryGetValue(className, out var migrations);

        return (classSymbol, attributeData, migrations);
    }

    private static Dictionary<string, Dictionary<int, AdditionalText>> ToMigrationFileSet(
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
            var fileName = Path.GetFileNameWithoutExtension(path);
            var regexMatch = SerializableMigrationSchema.MigrationFileRegex.Match(fileName);
            if (!regexMatch.Success)
            {
                continue;
            }

            var className = regexMatch.Captures[0].Value;
            if (!int.TryParse(regexMatch.Captures[1].Value, out var version))
            {
                continue;
            }

            var classMigrationSet = dictionary[className] ??= new Dictionary<int, AdditionalText>();
            classMigrationSet[version] = additionalText;
        }

        return dictionary;
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
        (INamedTypeSymbol, AttributeData, Dictionary<int, AdditionalText>?, ImmutableArray<(ISymbol, AttributeData)>) combinedClassData
    )
    {
        var (classSymbol, attributeData, migrations, fields) = combinedClassData;
        context.CancellationToken.ThrowIfCancellationRequested();
        var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions();

        string classSource = context.GenerateSerializationPartialClass(
            classSymbol,
            attributeData,
            jsonOptions,
            serializableList,
            embeddedSerializableList
        );

        if (classSource != null)
        {
            context.AddSource($"{classSymbol.ToDisplayString()}.Serialization.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }
}
