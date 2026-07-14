namespace DialogueDown.Visualization;

/// <summary>
/// The editor's completable names, resolved by the semantic analyzer and carried in the
/// report payload so the Source editor can complete from the real symbol table (canonical
/// speaker ids, merged tags, validated jump targets) instead of a text scan. Its shape
/// mirrors the browser's <c>DialogueSymbols</c>, so it deserializes straight into the
/// autocompletion seam.
/// </summary>
internal sealed record SymbolSet(
    IReadOnlyList<JumpTargetSymbol> JumpTargets,
    IReadOnlyList<string> Speakers,
    IReadOnlyList<string> SpeakerIds,
    IReadOnlyList<string> Tags);

/// <summary>One completable jump destination: a scene's anchor slug and its heading text.</summary>
internal sealed record JumpTargetSymbol(string Slug, string Heading);
