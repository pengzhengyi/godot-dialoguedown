using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Block-level dialogue content — the pieces that make up a script or scene body: a
/// <see cref="Line"/>, a group of <see cref="Choices"/>, or a <see cref="SceneHeading"/>.
/// A body keeps its blocks in source order; headings are flat markers, not containers.
/// </summary>
internal abstract record Block(SourceSpan Span) : ScriptNode(Span);
