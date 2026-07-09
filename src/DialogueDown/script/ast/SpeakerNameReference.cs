using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Points at a speaker by their display name.
/// </summary>
internal sealed record SpeakerNameReference(string Name, SourceSpan Span)
    : SpeakerReference(Span);
