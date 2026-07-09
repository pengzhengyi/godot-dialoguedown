using Spectre.Console;

namespace DialogueDown.Cli.Commands;

/// <summary>Validates the shared <c>&lt;script&gt;</c> argument that the commands take.</summary>
internal static class ScriptArgument
{
    /// <summary>The required extension for a DialogueDown script.</summary>
    public const string Extension = ".dialogue.md";

    /// <summary>
    /// Returns a validation error when <paramref name="script"/> is empty, has the
    /// wrong extension, or does not exist; otherwise success.
    /// </summary>
    public static ValidationResult Validate(string? script)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return ValidationResult.Error("A script path is required.");
        }

        if (!script.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Error(
                $"Expected a DialogueDown script ending in '{Extension}': {script}");
        }

        if (!File.Exists(script))
        {
            return ValidationResult.Error($"File not found: {script}");
        }

        return ValidationResult.Success();
    }
}
