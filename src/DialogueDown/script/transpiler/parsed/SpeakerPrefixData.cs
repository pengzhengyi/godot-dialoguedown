using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>
/// The parsed parts of a speaker prefix: an optional name, an optional id, and any
/// tags (each with its own span), plus the <c>:</c> separator's range. Unclassified — the
/// builder decides whether this is a declaration or a reference.
/// </summary>
internal sealed record SpeakerPrefixData(
    string? Name, string? Id, IReadOnlyList<Spanned<TagData>> Tags)
{
    /// <summary>The range of the terminating <c>:</c>, filled once the prefix's colon matches.</summary>
    public TextRange SeparatorRange { get; init; }
}
