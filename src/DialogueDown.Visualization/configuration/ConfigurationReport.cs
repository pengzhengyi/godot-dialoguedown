namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// The configuration section of the report payload: the optional
/// <see cref="ConfigurationFile"/> a compile applied and its resolved
/// <see cref="Speakers"/>. A null <see cref="File"/> is the no-config state (defaults
/// applied); the client shows the tab either way.
/// </summary>
internal sealed record ConfigurationReport(
    ConfigurationFile? File, IReadOnlyList<ConfiguredSpeakerView> Speakers);
