using System.ComponentModel;
using DialogueDown.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>Arguments and options for the <c>compile</c> command.</summary>
internal sealed class CompileSettings : CommandSettings
{
    // The --mode option's kebab-case values. Fail-fast is intentionally not offered: it throws at
    // the first error instead of collecting errata, so it is an embedding contract, not a CLI mode.
    private static readonly IReadOnlyDictionary<string, CompilationMode> Modes =
        new Dictionary<string, CompilationMode>(StringComparer.Ordinal)
        {
            ["stage-boundary"] = CompilationMode.StageBoundary,
            ["best-effort"] = CompilationMode.BestEffort,
        };

    [CommandArgument(0, "<script>")]
    [Description("The .dialogue.md script to compile.")]
    public string Script { get; init; } = string.Empty;

    [CommandOption("-o|--output <path>")]
    [Description("Write the compiled output to this path instead of standard output.")]
    public string? Output { get; init; }

    [CommandOption("--config <path>")]
    [Description("The dialogue.toml to configure the compile. Default: the nearest one found from the script's folder upward.")]
    public string? Config { get; init; }

    [CommandOption("--mode <mode>")]
    [Description("How far to compile after an error: stage-boundary (default) or best-effort.")]
    public string? Mode { get; init; }

    /// <summary>The compilation mode from <c>--mode</c>, or null to inherit the resolved options'
    /// mode. Only valid after <see cref="Validate"/> succeeds.</summary>
    public CompilationMode? ResolvedMode => Mode is null ? null : Modes[Mode];

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var script = ScriptArgument.Validate(Script);
        if (!script.Successful)
        {
            return script;
        }

        var config = ConfigArgument.Validate(Config);
        if (!config.Successful)
        {
            return config;
        }

        if (Mode is not null && !Modes.ContainsKey(Mode))
        {
            return ValidationResult.Error(
                $"Unknown --mode '{Mode}'. Use 'stage-boundary' or 'best-effort'.");
        }

        return ValidationResult.Success();
    }
}
