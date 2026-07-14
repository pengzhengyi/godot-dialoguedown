namespace DialogueDown.Configuration;

/// <summary>
/// Immutable options that configure a single compile. This is the public umbrella over the
/// compiler's per-stage option slices: a consumer builds it — in code, or later from a
/// <c>dialogue.toml</c> loader — and hands it to a composition root, which maps it to each
/// stage's slice. Start from <see cref="Default"/> and adjust it with a <c>with</c> expression.
/// </summary>
public sealed record CompilerOptions
{
    /// <summary>
    /// A fallback default-speaker name, used when a script declares no in-file default (the
    /// reserved <c>##default</c> tag). A blank or whitespace-only value counts as unset. Null
    /// by default, which keeps the anonymous default speaker.
    /// </summary>
    public string? DefaultSpeakerName { get; init; }

    /// <summary>The unconfigured options: every knob at its built-in default.</summary>
    public static CompilerOptions Default { get; } = new();

    /// <summary>
    /// The options slice the semantic analyzer reads. The configured default-speaker name is
    /// normalized here — a blank name becomes null (unset) — so the stage never sees a
    /// meaningless whitespace default.
    /// </summary>
    internal SemanticOptions Semantics =>
        new(string.IsNullOrWhiteSpace(DefaultSpeakerName) ? null : DefaultSpeakerName);
}
