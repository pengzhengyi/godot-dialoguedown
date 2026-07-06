using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One piece of a line's speech — plain text, styling, an image, a break, a game
/// call, and so on. Fragments stay granular at this stage; later stages coalesce
/// them into a rendered speech.
/// </summary>
internal abstract record SpeechFragment(SourceSpan Span) : ScriptNode(Span);
