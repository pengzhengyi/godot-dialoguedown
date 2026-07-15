using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Parses TOML text into Tomlyn's round-trippable syntax tree, bound to a source name so
/// diagnostics can point back at a file. It is the single seam that touches Tomlyn's parser;
/// syntax errors will surface here as located configuration failures once edge validation lands.
/// </summary>
internal sealed class TomlDocumentParser
{
    private readonly string _sourceName;

    public TomlDocumentParser(string sourceName) => _sourceName = sourceName;

    public DocumentSyntax Parse(string toml) => SyntaxParser.Parse(toml, _sourceName);
}
