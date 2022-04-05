using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SerializableMigration;

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
