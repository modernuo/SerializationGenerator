/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AdditionalTextSchemaFile.cs                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ModernUO.Serialization.Generator;

public class AdditionalTextSchemaFile : AdditionalText
{
    private SourceText _sourceText;

    public override string Path { get; }

    public override SourceText? GetText(CancellationToken cancellationToken) => _sourceText;

    public AdditionalTextSchemaFile(string path, SourceText text)
    {
        Path = path;
        _sourceText = text;
    }
}
