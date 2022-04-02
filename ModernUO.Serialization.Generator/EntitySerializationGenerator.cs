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

using System.Collections.Immutable;
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
        var cancelToken = new CancellationTokenSource();

        var serializableClasses = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsClassSyntaxNode,
                GetSerializableClassDeclaration
            )
            .Where(t => t != null);

        /*
         * 1. Gather all ISerializable
         * 2. Gather all EmbeddedSerializable
         * 3. Gather all "Additional Files" from migrations by path
         * 4. Transform the file paths to source for that class
         * 5. Register for output to source
         */
    }

    public static bool IsClassSyntaxNode(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    public static (ClassDeclarationSyntax, AttributeData)? GetSerializableClassDeclaration(GeneratorSyntaxContext ctx, CancellationToken token)
    {
        var classNode = (ClassDeclarationSyntax)ctx.Node;
        var classSymbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(classNode);

        if (classSymbol.IsSerializableClass(ctx.SemanticModel.Compilation, out var attributeData))
        {
            return (classNode, attributeData);
        }

        return null;
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SerializerSyntaxReceiver receiver)
        {
            return;
        }

        var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions();
        // List of types that _will_ become ISerializable
        var serializableList = receiver.SerializableList;
        var embeddedSerializableList = receiver.EmbeddedSerializableList;

        foreach (var (classSymbol, (serializableAttr, fieldsList)) in receiver.ClassAndFields)
        {
            if (serializableAttr == null)
            {
                continue;
            }

            string classSource = context.GenerateSerializationPartialClass(
                classSymbol,
                serializableAttr,
                false,
                fieldsList.ToImmutableArray(),
                jsonOptions,
                serializableList,
                embeddedSerializableList
            );

            if (classSource != null)
            {
                context.AddSource($"{classSymbol.ToDisplayString()}.Serialization.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        foreach (var (classSymbol, (embeddedSerializableAttr, fieldsList)) in receiver.EmbeddedClassAndFields)
        {
            if (embeddedSerializableAttr == null)
            {
                continue;
            }

            string classSource = context.GenerateSerializationPartialClass(
                classSymbol,
                embeddedSerializableAttr,
                true,
                fieldsList.ToImmutableArray(),
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
}
