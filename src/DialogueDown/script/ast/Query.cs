using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A game-state read. The key is looked up at run time and the returned value is
/// inserted into speech.
/// </summary>
internal sealed record Query(string Key, SourceSpan Span) : GameCall(Span);
