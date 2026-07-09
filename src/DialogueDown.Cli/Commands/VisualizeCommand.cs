using DialogueDown.Cli.Compilation;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>visualize</c> command: compile a script through the seam, then (later)
/// render its stages. It <b>relies on the compilation seam</b> rather than compiling
/// the script itself. The rendering arrives with the visualization component; until
/// then this reports "not implemented".
/// </summary>
internal sealed class VisualizeCommand : Command<VisualizeSettings>
{
    private readonly IScriptCompiler _compiler;

    public VisualizeCommand(IScriptCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        _compiler = compiler;
    }

    /// <inheritdoc />
    protected override int Execute(
        CommandContext context, VisualizeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var source = File.ReadAllText(settings.Script);
        _compiler.Compile(source);

        // TODO(visualization): render the compiled stages into a report.
        return ExitCodes.Success;
    }
}
