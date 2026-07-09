using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A speaker given by an id together with extra tags, but no name — a reference that
/// also contributes tags (for example <c>@alice #excited:</c>). A later stage resolves
/// the id to the declared speaker and merges in the tags.
/// </summary>
internal sealed record PartialSpeakerDeclaration(
    string Id, IReadOnlyList<Tag> Tags, SourceSpan Span) : Speaker(Span);
