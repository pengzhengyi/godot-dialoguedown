using DialogueDown.Cli.Compilation;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: compile a script through the compilation seam. The
/// seam is not implemented yet, so this reports "not implemented" until the
/// transpiler lands; the command body then needs no change.
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

        // TODO(transpiler): emit the compiled result, honouring --output.
        return ExitCodes.Success;
    }
}
