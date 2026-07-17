using Spectre.Console;

namespace DialogueDown.Cli.Commands;

/// <summary>Validates the shared optional <c>--config</c> option the commands take.</summary>
internal static class ConfigArgument
{
    /// <summary>
    /// Returns a validation error when <paramref name="config"/> is given but blank or names a
    /// file that does not exist; a null value (no <c>--config</c>) is valid — the CLI discovers
    /// a <c>dialogue.toml</c> instead.
    /// </summary>
    public static ValidationResult Validate(string? config)
    {
        if (config is null)
        {
            return ValidationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(config))
        {
            return ValidationResult.Error("--config requires a path to a dialogue.toml.");
        }

        return File.Exists(config)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Config file not found: {config}");
    }
}
