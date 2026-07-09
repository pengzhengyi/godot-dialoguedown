using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>Arguments and options for the <c>visualize</c> command.</summary>
internal sealed class VisualizeSettings : CommandSettings
{
    [CommandArgument(0, "<script>")]
    [Description("The .dialogue.md script to visualize.")]
    public string Script { get; init; } = string.Empty;

    /// <inheritdoc />
    public override ValidationResult Validate() => ScriptArgument.Validate(Script);
}
