using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Points at a speaker by their stable id.
/// </summary>
internal sealed record SpeakerIdReference(string Id, SourceSpan Span)
    : SpeakerReference(Span);
