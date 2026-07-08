using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// An inline link. <see cref="Target"/> is where it points, kept as a plain string
/// that a later stage resolves; it may be empty (an unresolved target) but never null.
/// <see cref="Label"/> is the shown text as an ordered fragment sequence, so it can
/// carry <see cref="Tag"/>s and styling like any other speech.
/// </summary>
internal sealed record Link : InlineFragment
{
    public Link(string target, IReadOnlyList<InlineFragment> label, SourceSpan span)
        : base(span)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
        Label = label;
    }

    public string Target { get; }

    public IReadOnlyList<InlineFragment> Label { get; }
}
