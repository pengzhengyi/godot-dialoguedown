using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Who speaks a line, in one of two forms: a <see cref="SpeakerDeclaration"/> that
/// binds identity and metadata, or a <see cref="SpeakerReference"/> that only
/// points at a speaker. Unresolved at this stage — a later stage resolves it and
/// fills a default when a line names no speaker.
/// </summary>
internal abstract record Speaker(SourceSpan Span) : ScriptNode(Span);
