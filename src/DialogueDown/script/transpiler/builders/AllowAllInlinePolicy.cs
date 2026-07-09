using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The policy for a line's speech: every inline element is supported and a <c>=&gt;</c>
/// is a jump. Nothing is ever unsupported, so <see cref="Resolve"/> is unreachable.
/// </summary>
internal sealed class AllowAllInlinePolicy : IInlinePolicy
{
    public static AllowAllInlinePolicy Instance { get; } = new();

    public bool SupportsJumps => true;

    public bool Supports(MarkdownInline inline) => true;

    public IReadOnlyList<InlineFragment> Resolve(MarkdownInline unsupported) =>
        throw new InvalidOperationException(
            "Speech supports every inline element, so Resolve should never be called.");
}
