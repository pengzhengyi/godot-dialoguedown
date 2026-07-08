using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One selectable option at a branch. It holds its own <see cref="Body"/> blocks, so a
/// choice can carry a <see cref="Line"/> and a nested <see cref="Choices"/> — which is
/// how nested choices are represented. A choice is not itself a <see cref="Block"/>.
/// </summary>
internal sealed record Choice(IReadOnlyList<Block> Body, SourceSpan Span) : ScriptNode(Span);
