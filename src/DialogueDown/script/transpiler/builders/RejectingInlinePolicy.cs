using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Errors;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The strict policy for an image alt or a link label: text and styling are supported,
/// and any functional element — a code span, link, image, or break — is a
/// <see cref="DialogueSyntaxError"/> rather than being restored to text. Use it to hold
/// authors to labels that carry only words and styling.
/// </summary>
internal sealed class RejectingInlinePolicy : IInlinePolicy
{
    public bool SupportsJumps => false;

    public bool Supports(MarkdownInline inline) => inline is TextInline or EmphasisInline;

    public IReadOnlyList<InlineFragment> Resolve(MarkdownInline unsupported) =>
        throw new DialogueSyntaxError(
            $"{Describe(unsupported)} is not allowed inside a label or alt text; " +
            "only text and styling are.",
            unsupported.Span);

    private static string Describe(MarkdownInline inline) => inline switch
    {
        CodeSpanInline => "a game call",
        ImageInline => "an image",
        LinkInline => "a link",
        MarkdownLineBreak => "a line break",
        _ => "this element",
    };
}
