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

    [CommandOption("--watch")]
    [Description("Serve the report from a local server and hot-reload it on file changes.")]
    public bool Watch { get; init; }

    [CommandOption("-o|--output <path>")]
    [Description("Write the report to this path instead of a temp file (static mode).")]
    public string? Output { get; init; }

    [CommandOption("--port <port>")]
    [Description("The loopback port for --watch (default: an ephemeral port).")]
    public int? Port { get; init; }

    [CommandOption("--no-open")]
    [Description("Do not open the report in the browser.")]
    public bool NoOpen { get; init; }

    [CommandOption("--render-root <dir>")]
    [Description(
        "Serve static assets from this folder (must contain the script) so images "
        + "outside the script's folder resolve. Skips the hosting-consent prompt.")]
    public string? RenderRoot { get; init; }

    /// <inheritdoc />
    public override ValidationResult Validate() => ScriptArgument.Validate(Script);
}
