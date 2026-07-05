namespace DialogueDown.Markdown;

/// <summary>
/// Plain text exactly as the author wrote it, with no styling applied. It always
/// has at least one character.
/// </summary>
internal sealed record TextInline : MarkdownInline
{
    public TextInline(string text, SourceSpan span)
        : base(span)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        Text = text;
    }

    public string Text { get; }
}
