namespace DialogueDown.Markdown;

/// <summary>
/// A heading line (<c>#</c> to <c>######</c>). <see cref="Level"/> is 1 through 6,
/// matching the number of leading hashes.
/// </summary>
internal sealed record Heading : MarkdownBlock
{
    public Heading(int level, IReadOnlyList<MarkdownInline> inlines, SourceSpan span)
        : base(span)
    {
        AssertValidHeadingLevel(level);
        Level = level;
        Inlines = inlines;
    }

    public int Level { get; }

    public IReadOnlyList<MarkdownInline> Inlines { get; }

    private static void AssertValidHeadingLevel(int level)
    {
        if (level is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level), level, "Heading level must be between 1 and 6.");
        }
    }
}
