using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The root of the Dialogue AST: the whole script as an ordered <see cref="Body"/> of
/// blocks in source order, headings included as flat <see cref="SceneHeading"/> markers;
/// grouping them into scenes is a later stage's job. Named <c>ScriptDocument</c> (not
/// <c>Script</c>) so the root type does not collide with the <c>Script</c> namespace,
/// mirroring how the front-end names its root <c>MarkdownDocument</c>.
/// </summary>
internal sealed record ScriptDocument(IReadOnlyList<Block> Body, SourceSpan Span) : ScriptNode(Span);
