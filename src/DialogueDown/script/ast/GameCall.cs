using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A call into the game system embedded in a line. A <see cref="Query"/> reads
/// game state; a command changes it. The call is recognized here and run later
/// against the game system.
/// </summary>
internal abstract record GameCall(SourceSpan Span) : InlineFragment(Span);
