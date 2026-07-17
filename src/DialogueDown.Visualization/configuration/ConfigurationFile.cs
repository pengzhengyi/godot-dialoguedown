namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// A configuration file a compile applied: the <see cref="Path"/> it was read from and its
/// raw TOML <see cref="Source"/> text. The two travel together — a file always has both — so
/// a report can show the exact configuration a reader authored.
/// </summary>
internal sealed record ConfigurationFile(string Path, string Source);
