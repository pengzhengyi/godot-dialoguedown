using DialogueDown.Markdown;

namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// A hand-written <see cref="IMarkdownParser"/> that returns a preset document and
/// records the source it was given, so a facade test can inject a known AST without
/// running the real parser.
/// </summary>
internal sealed class StubMarkdownParser : IMarkdownParser
{
    private readonly MarkdownDocument _document;

    public StubMarkdownParser(MarkdownDocument document) => _document = document;

    public string? ReceivedSource { get; private set; }

    public MarkdownDocument Parse(string source)
    {
        ReceivedSource = source;
        return _document;
    }
}
