using DialogueDown.Configuration;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads a DialogueDown project's <c>dialogue.toml</c> into a <see cref="CompilerOptions"/> so the
/// engine-agnostic core never takes a TOML dependency. It is a thin composition root: it parses
/// the text with <see cref="TomlDocumentParser"/> and maps the speakers with
/// <see cref="ConfiguredSpeakerReader"/>. A config with no <c>[[speakers]]</c> yields
/// <see cref="CompilerOptions.Default"/>.
/// </summary>
public static class TomlConfigurationLoader
{
    /// <summary>
    /// Reads and parses the <c>dialogue.toml</c> at <paramref name="path"/>, which also names the
    /// source in diagnostics. A missing file surfaces the underlying IO exception.
    /// </summary>
    public static CompilerOptions Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Parse(File.ReadAllText(path), path);
    }

    /// <summary>
    /// Parses <paramref name="toml"/> into a <see cref="CompilerOptions"/>. The required
    /// <paramref name="sourceName"/> names the source in diagnostics (a file path, or a synthetic
    /// name for in-memory config).
    /// </summary>
    internal static CompilerOptions Parse(string toml, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(toml);
        ArgumentNullException.ThrowIfNull(sourceName);

        DocumentSyntax document = new TomlDocumentParser(sourceName).Parse(toml);
        IReadOnlyList<ConfiguredSpeaker> speakers = new ConfiguredSpeakerReader().Read(document);
        return speakers.Count == 0 ? CompilerOptions.Default : new CompilerOptions { Speakers = speakers };
    }
}
