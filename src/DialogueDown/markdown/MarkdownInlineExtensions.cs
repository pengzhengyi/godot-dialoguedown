using DialogueDown.Common;

namespace DialogueDown.Markdown;

/// <summary>
/// Reusable transformations over a Markdown inline sequence, shared by any construct that
/// reshapes inline content.
/// </summary>
internal static class MarkdownInlineExtensions
{
    /// <summary>
    /// The inlines with any leading whitespace removed from the first text inline, re-anchoring
    /// its span; a whitespace-only leading text inline is dropped entirely. This lets a caller
    /// that peels a leading inline — such as a random choice's weight — leave clean content
    /// behind, so the remainder reads as if the peeled inline were never there.
    /// </summary>
    public static IReadOnlyList<MarkdownInline> TrimLeadingWhitespace(
        this IEnumerable<MarkdownInline> inlines)
    {
        var list = inlines as IReadOnlyList<MarkdownInline> ?? inlines.ToList();
        if (list is not [TextInline text, ..] || !text.Text.HasLeadingWhitespace())
        {
            return list;
        }

        var trimmed = text.Text.TrimStart();
        var removed = text.Text.Length - trimmed.Length;
        var rest = list.Skip(1);
        if (trimmed.Length == 0)
        {
            return rest.ToList();
        }

        var head = new TextInline(
            trimmed,
            new SourceSpan(text.Span.Start + removed, trimmed.Length),
            new SourceSpan(text.ContentSpan.Start + removed, trimmed.Length));
        return [head, .. rest];
    }
}
