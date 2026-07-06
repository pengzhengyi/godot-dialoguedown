using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The base for every Dialogue AST node. Each node carries the
/// <see cref="SourceSpan"/> of the source it came from, so diagnostics and tooling
/// can point back at the exact characters.
/// </summary>
internal abstract record ScriptNode(SourceSpan Span);
