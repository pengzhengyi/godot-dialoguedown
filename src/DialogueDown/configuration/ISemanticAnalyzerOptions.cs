namespace DialogueDown.Configuration;

/// <summary>
/// The options the semantic analysis stage reads. A per-stage view that
/// <see cref="CompilerOptions"/> separates from the umbrella, so the analyzer depends only on
/// the options it uses rather than the whole configuration.
/// </summary>
internal interface ISemanticAnalyzerOptions
{
    /// <summary>The speakers supplied by configuration, seeded alongside a script's own speakers.</summary>
    IReadOnlyList<ConfiguredSpeaker> ConfiguredSpeakers { get; }
}
