namespace DialogueDown.Visualization;

/// <summary>
/// An overlay applied to a served report when the configuration on disk is persisted but invalid
/// (a saved-invalid Config). The report's graphs and speakers still reflect the last valid
/// compile, but the Config tab must show the current invalid <see cref="Source"/> and the report
/// must announce that its compiled configuration is stale, so a page reload restores the
/// saved-invalid state rather than silently reverting to the last valid text.
/// </summary>
/// <param name="Source">The current, invalid configuration TOML persisted on disk.</param>
/// <param name="Message">The parse error explaining why the configuration is invalid.</param>
public sealed record ConfigStatusOverlay(string Source, string Message);
