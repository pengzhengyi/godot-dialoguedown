using Markdig;
using Markdig.Parsers.Inlines;

namespace DialogueSystem.Markdown;

/// <summary>
/// Parses a script with the Markdig library and converts its tree into our own
/// Markdown AST. The pipeline is kept minimal (plain CommonMark, no GitHub
/// extensions) so script text is not reinterpreted as tables and the like, and
/// emphasis parsing is turned off so styling markers stay part of the raw text.
/// </summary>
internal sealed class MarkdigMarkdownParser : IMarkdownParser
{
    private static readonly MarkdownPipeline _pipeline = BuildPipeline();

    public MarkdownDocument Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var parsed = Markdig.Markdown.Parse(source, _pipeline);
        return new MarkdigToMarkdownAstConverter(source).Convert(parsed);
    }

    private static MarkdownPipeline BuildPipeline()
    {
        var builder = new MarkdownPipelineBuilder().UsePreciseSourceLocation();
        builder.InlineParsers.TryRemove<EmphasisInlineParser>();
        return builder.Build();
    }
}
