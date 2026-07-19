using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The strict policy for an image alt or a link label: text and styling are supported,
/// and any functional element — a code span, link, image, or break — reports
/// <see cref="DiagnosticCatalog.DisallowedLabelElement"/> and is dropped rather than being
/// restored to text. Use it to hold authors to labels that carry only words and styling.
/// </summary>
internal sealed class RejectingInlinePolicy : IInlinePolicy
{
    public bool SupportsJumps => false;

    public bool Supports(MarkdownInline inline) => inline is TextInline or EmphasisInline;

    public IReadOnlyList<InlineFragment> Resolve(
        MarkdownInline unsupported, IDiagnosticSink diagnostics)
    {
        // Report and recover by dropping the element, so the label keeps only its words and
        // styling and the rest of the line still compiles.
        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.DisallowedLabelElement, unsupported.Span, [Describe(unsupported)]));
        return [];
    }

    private static string Describe(MarkdownInline inline) => inline switch
    {
        CodeSpanInline => "a game call",
        ImageInline => "an image",
        LinkInline => "a link",
        MarkdownLineBreak => "a line break",
        _ => "this element",
    };
}
