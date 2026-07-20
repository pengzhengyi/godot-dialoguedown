namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// The configuration section of the report payload: the optional
/// <see cref="ConfigurationFile"/> a compile applied, the project's compilation <see cref="Mode"/>
/// (its author-facing name, for display), its resolved <see cref="Speakers"/>, and the
/// <see cref="ReservedTags"/> the compiler recognizes (for the editor's autocompletion). A
/// null <see cref="File"/> is the no-config state (defaults applied); the client shows the tab
/// either way.
/// </summary>
internal sealed record ConfigurationReport(
    ConfigurationFile? File,
    string Mode,
    IReadOnlyList<ConfiguredSpeakerView> Speakers,
    IReadOnlyList<string> ReservedTags);
