using Markdig;

namespace DialogueSystem.Markdown;

/// <summary>
/// Parses a script with the Markdig library and converts its tree into our own
/// Markdown AST. The pipeline is kept minimal (plain CommonMark, no GitHub
/// extensions) so script text is not reinterpreted as tables and the like.
/// </summary>
internal sealed class MarkdigMarkdownParser : IMarkdownParser
{
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();

    private readonly MarkdownAstMapper _mapper = new();

    public MarkdownDocument Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var parsed = Markdig.Markdown.Parse(source, _pipeline);
        return _mapper.Map(parsed);
    }
}
