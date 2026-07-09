using System.Reflection;
using Spectre.Console.Cli;

namespace DialogueDown.Cli;

/// <summary>
/// Configures the command app: application name, version, and the subcommands. Shared
/// by the real runner and the tests so both exercise the same command wiring.
/// </summary>
internal static class CliConfigurator
{
    /// <summary>Applies the CLI configuration (name, version, commands).</summary>
    public static void Configure(IConfigurator config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.SetApplicationName("dialoguedown");
        config.SetApplicationVersion(ResolveVersion());
    }

    /// <summary>The tool version, read from the assembly (set by <c>&lt;Version&gt;</c>).</summary>
    private static string ResolveVersion()
    {
        var informational = typeof(CliConfigurator).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
        {
            // Drop any build metadata (e.g. a "+<commit>" suffix from SourceLink).
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informational[..plus] : informational;
        }

        return typeof(CliConfigurator).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
