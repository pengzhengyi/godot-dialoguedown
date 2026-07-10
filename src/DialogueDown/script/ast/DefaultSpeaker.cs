using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The default speaker: who speaks a line that names no speaker. A sentinel that a later
/// stage resolves to the concrete default speaker, or to the system speaker when none is
/// declared.
/// </summary>
internal sealed record DefaultSpeaker(SourceSpan Span) : Speaker(Span);
