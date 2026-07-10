using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The policy for a scene heading's title: every inline element is supported, but a
/// <c>=&gt;</c> is plain text, not a jump. A heading becomes a scene, which is itself a
/// jump target, so jumping from inside one is meaningless. Nothing is ever unsupported,
/// so <see cref="Resolve"/> is unreachable.
/// </summary>
internal sealed class TitleInlinePolicy : IInlinePolicy
{
    public static TitleInlinePolicy Instance { get; } = new();

    public bool SupportsJumps => false;

    public bool Supports(MarkdownInline inline) => true;

    public IReadOnlyList<InlineFragment> Resolve(MarkdownInline unsupported) =>
        throw new InvalidOperationException(
            "A heading title supports every inline element, so Resolve should never be called.");
}
