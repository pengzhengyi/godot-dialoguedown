namespace DialogueDown.Configuration;

/// <summary>
/// The per-stage options slice the semantic analyzer reads. It carries only the knobs that
/// stage needs — today the normalized configured default-speaker name, null when unset — so
/// the analyzer depends on this small contract rather than the whole <see cref="CompilerOptions"/>.
/// </summary>
internal sealed record SemanticOptions(string? DefaultSpeakerName);
