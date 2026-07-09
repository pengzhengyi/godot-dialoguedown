using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Who speaks a line, in one of a few forms: a <see cref="SpeakerDeclaration"/> that
/// binds identity and metadata, a <see cref="SpeakerReference"/> that only points at
/// a speaker, or a <see cref="PartialSpeakerDeclaration"/> that points at one by id
/// while contributing extra tags. Unresolved at this stage — a later stage resolves
/// it and fills a default when a line names no speaker.
/// </summary>
internal abstract record Speaker(SourceSpan Span) : ScriptNode(Span);
