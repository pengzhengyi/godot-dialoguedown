using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A soft line break kept as a hint that downstream display may wrap here. A hard
/// break is consumed as a Line boundary instead and never appears as a fragment.
/// </summary>
internal sealed record LineBreak(SourceSpan Span) : SpeechFragment(Span);
