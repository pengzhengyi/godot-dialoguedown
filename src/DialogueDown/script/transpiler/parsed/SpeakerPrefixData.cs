using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>
/// The parsed parts of a speaker prefix: an optional name, an optional id, and any
/// tags (each with its own span). Unclassified — the builder decides whether this is
/// a declaration or a reference.
/// </summary>
internal sealed record SpeakerPrefixData(
    string? Name, string? Id, IReadOnlyList<Spanned<TagData>> Tags);
