namespace DialogueSystem.Markdown;

/// <summary>
/// A paragraph: one block of text made up of inline pieces.
/// </summary>
internal sealed record Paragraph(IReadOnlyList<MarkdownInline> Inlines, SourceSpan Span)
    : MarkdownBlock(Span);
