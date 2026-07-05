using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A speaker prefix that binds identity and metadata: a required display name, an
/// optional stable id, and any tags. Introducing metadata is what makes a prefix
/// a declaration rather than a bare reference.
/// </summary>
internal sealed record SpeakerDeclaration(
    string Name, string? Id, IReadOnlyList<Tag> Tags, SourceSpan Span) : Speaker(Span);
