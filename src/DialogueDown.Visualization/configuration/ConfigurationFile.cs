namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// A configuration file a compile applied: the <see cref="Path"/> it was read from and its
/// raw TOML <see cref="Source"/> text. The two travel together — a file always has both — so
/// a report can show the exact configuration a reader authored.
/// </summary>
public sealed record ConfigurationFile(string Path, string Source)
{
    /// <summary>
    /// The conventional file name a DialogueDown project's configuration lives in — the single
    /// source of truth shared by config discovery and the create-new flow.
    /// </summary>
    public const string DefaultName = "dialogue.toml";
}
