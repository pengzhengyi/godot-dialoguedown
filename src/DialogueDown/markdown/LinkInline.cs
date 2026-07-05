namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown link. <see cref="Target"/> is where the link points and
/// <see cref="Label"/> is the text shown for it; both are kept exactly as
/// written. The dialogue compiler later reads the target when a link forms a
/// jump.
/// </summary>
internal sealed record LinkInline(string Target, string Label, SourceSpan Span)
    : MarkdownInline(Span);
