using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Speech text with a style applied — italic, bold, or strikethrough (see
/// <see cref="Style"/>). It nests <see cref="Children"/> fragments, so bold text can
/// itself contain a query, a nested style, or any other fragment. It records only that
/// the text is styled; how it renders is decided downstream. It always wraps at least
/// one fragment: empty styling (like <c>****</c>) is never produced — the source keeps
/// it as plain text.
/// </summary>
internal sealed record StyledText : InlineFragment
{
    public StyledText(SpeechStyle style, IReadOnlyList<InlineFragment> children, SourceSpan span)
        : base(span)
    {
        AssertHasContent(children);
        Style = style;
        Children = children;
    }

    public SpeechStyle Style { get; }

    public IReadOnlyList<InlineFragment> Children { get; }

    private static void AssertHasContent(IReadOnlyList<InlineFragment> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException(
                "Styled text must wrap at least one fragment; empty styling is never produced.",
                nameof(children));
        }
    }
}
