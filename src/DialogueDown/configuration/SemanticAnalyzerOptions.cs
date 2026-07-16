namespace DialogueDown.Configuration;

/// <summary>
/// The default <see cref="ISemanticAnalyzerOptions"/>: a straight view over the configured
/// speakers that <see cref="CompilerOptions"/> hands to the semantic analysis stage.
/// </summary>
internal sealed record SemanticAnalyzerOptions(IReadOnlyList<ConfiguredSpeaker> ConfiguredSpeakers)
    : ISemanticAnalyzerOptions;
