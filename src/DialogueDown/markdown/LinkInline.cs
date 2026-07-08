using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown link. <see cref="Target"/> is where the link points, kept exactly as
/// written. <see cref="Label"/> is the shown content as inline nodes, so a label can
/// carry styling and other inlines (for example <c>[**bold** link](url)</c>). The
/// dialogue compiler later reads the target when a link forms a jump.
/// </summary>
internal sealed record LinkInline(string Target, IReadOnlyList<MarkdownInline> Label, SourceSpan Span)
    : MarkdownInline(Span);
