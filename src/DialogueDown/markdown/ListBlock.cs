namespace DialogueDown.Markdown;

/// <summary>
/// A list of items. <see cref="IsOrdered"/> records whether the source used
/// numbers (<c>1.</c>) or bullets (<c>-</c>); later stages may ignore it.
/// </summary>
internal sealed record ListBlock(bool IsOrdered, IReadOnlyList<ListItem> Items, SourceSpan Span)
    : MarkdownBlock(Span);
