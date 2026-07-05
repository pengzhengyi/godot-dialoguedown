using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds Dialogue AST nodes for tests with spans and sensible defaults filled in,
/// so a test only states the parts it cares about.
/// </summary>
internal static class DialogueAstFactory
{
    public static CustomTag CustomTag(string name, string? value = null) =>
        new(name, value, SourceSpanFactory.Span());

    public static ReservedTag ReservedTag(string name, string? value = null) =>
        new(name, value, SourceSpanFactory.Span());

    public static IReadOnlyList<Tag> Tags(params Tag[] tags) => tags;

    public static SpeakerDeclaration SpeakerDeclaration(
        string name, string? id = null, params Tag[] tags) =>
        new(name, id, tags, SourceSpanFactory.Span());

    public static SpeakerNameReference SpeakerNameReference(string name) =>
        new(name, SourceSpanFactory.Span());

    public static SpeakerIdReference SpeakerIdReference(string id) =>
        new(id, SourceSpanFactory.Span());
}
