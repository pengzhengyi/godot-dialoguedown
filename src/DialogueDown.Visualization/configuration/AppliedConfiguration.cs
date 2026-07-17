using DialogueDown.Configuration;

namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// The configuration a compile applied, as shown in the report: an optional
/// <see cref="ConfigurationFile"/> together with the resolved <see cref="CompilerOptions"/>.
/// A null <see cref="File"/> means no <c>dialogue.toml</c> was found — the compile ran on the
/// built-in defaults. Read the state through <see cref="IsConfiguredFromFile"/> and
/// <see cref="UsesDefaultConfiguration"/> rather than testing the field, so the nullable stays
/// an implementation detail.
/// </summary>
public sealed record AppliedConfiguration(ConfigurationFile? File, CompilerOptions Options)
{
    /// <summary>A <c>dialogue.toml</c> was found and applied.</summary>
    public bool IsConfiguredFromFile => File is not null;

    /// <summary>No configuration file was found; the compile used the built-in defaults.</summary>
    public bool UsesDefaultConfiguration => File is null;

    /// <summary>The applied configuration for a compile that read a <paramref name="path"/>.</summary>
    public static AppliedConfiguration FromFile(string path, string source, CompilerOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        return new AppliedConfiguration(new ConfigurationFile(path, source), options);
    }

    /// <summary>The applied configuration for a compile that found no file — the defaults.</summary>
    public static AppliedConfiguration WithoutFile(CompilerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new AppliedConfiguration(null, options);
    }
}
