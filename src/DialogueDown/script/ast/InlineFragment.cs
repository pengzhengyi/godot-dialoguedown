using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One piece of inline content — plain text, styling, an image, a break, a game call,
/// and so on. It appears in a line's speech and inside an image alt or link label.
/// Fragments stay granular at this stage; later stages coalesce them into rendered
/// content.
/// </summary>
internal abstract record InlineFragment(SourceSpan Span) : ScriptNode(Span);
