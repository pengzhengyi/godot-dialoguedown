using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A speaker prefix that only points at a speaker, without binding metadata —
/// either by name (<see cref="SpeakerNameReference"/>) or by stable id
/// (<see cref="SpeakerIdReference"/>).
/// </summary>
internal abstract record SpeakerReference(SourceSpan Span) : Speaker(Span);
