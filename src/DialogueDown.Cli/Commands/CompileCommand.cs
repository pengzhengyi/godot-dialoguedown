using DialogueDown.Compilation;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: compile a script through the compiler facade. The
/// compiled output is not emitted yet; the command reports success and will honor
/// <c>--output</c> once the later stages produce something to write.
/// </summary>
internal sealed class CompileCommand : Command<CompileSettings>
{
    private readonly IScriptCompiler _compiler;

    public CompileCommand(IScriptCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        _compiler = compiler;
    }

    /// <inheritdoc />
    protected override int Execute(
        CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var source = File.ReadAllText(settings.Script);
        _compiler.Compile(source);

        // TODO(compiler): emit the compiled output, honoring --output, once the later
        // stages produce a serializable result.
        return ExitCodes.Success;
    }
}
