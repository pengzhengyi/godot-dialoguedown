using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>Arguments and options for the <c>compile</c> command.</summary>
internal sealed class CompileSettings : CommandSettings
{
    [CommandArgument(0, "<script>")]
    [Description("The .dialogue.md script to compile.")]
    public string Script { get; init; } = string.Empty;

    [CommandOption("-o|--output <path>")]
    [Description("Write the compiled output to this path instead of standard output.")]
    public string? Output { get; init; }

    [CommandOption("--config <path>")]
    [Description("The dialogue.toml to configure the compile. Default: the nearest one found from the script's folder upward.")]
    public string? Config { get; init; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var script = ScriptArgument.Validate(Script);
        return script.Successful ? ConfigArgument.Validate(Config) : script;
    }
}
