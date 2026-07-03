namespace DialogueSystem.Markdown;

/// <summary>
/// Text between backticks. The inner text is kept exactly as written; the
/// dialogue compiler later reads it as a query or command.
/// </summary>
internal sealed record CodeSpanInline(string Content, SourceSpan Span)
    : MarkdownInline(Span);
