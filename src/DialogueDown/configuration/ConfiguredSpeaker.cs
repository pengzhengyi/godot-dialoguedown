namespace DialogueDown.Configuration;

/// <summary>
/// A speaker supplied by configuration rather than declared in a script: a display name, an
/// optional stable id, and its tags partitioned into custom and reserved. It is plain data,
/// validated and partitioned at the configuration edge; the semantic stage turns it into a
/// speaker declaration to bind alongside the script's own speakers. A speaker is the default
/// when its <see cref="ReservedTags"/> include the <c>default</c> reserved tag.
/// </summary>
public sealed record ConfiguredSpeaker(
    string Name,
    string? Id,
    IReadOnlyList<ConfiguredTag> CustomTags,
    IReadOnlyList<ConfiguredTag> ReservedTags);
