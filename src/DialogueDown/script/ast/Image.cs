using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// An inline image. <see cref="Source"/> is where the image is loaded from, kept as
/// a plain string that a later stage resolves; it may be empty (an unresolved source)
/// but never null. <see cref="Alt"/> is its alternative text as an ordered fragment
/// sequence, so the label can carry <see cref="Tag"/>s and styling like any other
/// speech.
/// </summary>
internal sealed record Image : InlineFragment
{
    public Image(string source, IReadOnlyList<InlineFragment> alt, SourceSpan span)
        : base(span)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
        Alt = alt;
    }

    public string Source { get; }

    public IReadOnlyList<InlineFragment> Alt { get; }
}
