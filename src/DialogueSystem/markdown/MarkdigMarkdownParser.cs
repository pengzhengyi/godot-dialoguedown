using Markdig;

namespace DialogueSystem.Markdown;

/// <summary>
/// Parses a script with the Markdig library and converts its tree into our own
/// Markdown AST. The pipeline is stock CommonMark (no GitHub extensions), so
/// script text is not reinterpreted as tables and the like; emphasis is parsed so
/// that italic/bold styling can be modeled and interpreted downstream.
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

    private static MarkdownPipeline BuildPipeline() =>
        new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();
}
