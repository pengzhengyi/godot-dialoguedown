using DialogueDown.Markdown;

namespace DialogueDown.Visualization;

/// <summary>
/// Ergonomic entry point for the Markdown stage: turns a parsed
/// <see cref="MarkdownDocument"/> into a display graph without naming the
/// projection.
/// </summary>
internal static class MarkdownDisplayExtensions
{
    public static DisplayGraph ToDisplayGraph(this MarkdownDocument document) =>
        GraphWalk.Walk<object>(document, new MarkdownAstProjection());
}
