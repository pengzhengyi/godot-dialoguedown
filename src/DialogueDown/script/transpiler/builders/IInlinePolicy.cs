using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The rule for which inline elements a context admits, and what an inadmissible one
/// becomes. Speech admits everything; an image alt or a link label admits only text,
/// tags, and styling. Because the decision is made on the Markdown input before it is
/// converted, the policy inspects the <see cref="MarkdownInline"/> directly rather than
/// a dialogue kind.
/// </summary>
internal interface IInlinePolicy
{
    /// <summary>
    /// Whether a <c>=&gt;</c> written in text is a jump here. Jumps live inside a text
    /// piece, not as their own <see cref="MarkdownInline"/>, so they need their own gate.
    /// </summary>
    bool SupportsJumps { get; }

    /// <summary>Whether this context builds <paramref name="inline"/> into a fragment.</summary>
    bool Supports(MarkdownInline inline);

    /// <summary>
    /// Produces the fragments an unsupported inline becomes, reporting into
    /// <paramref name="diagnostics"/> when the resolution is a recovered error. Called only when
    /// <see cref="Supports"/> is <see langword="false"/>.
    /// </summary>
    IReadOnlyList<InlineFragment> Resolve(MarkdownInline unsupported, IDiagnosticSink diagnostics);
}
