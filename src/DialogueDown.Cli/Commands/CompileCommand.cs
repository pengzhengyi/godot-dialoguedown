using DialogueDown.Compilation;
using DialogueDown.Configuration;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: resolve the project's <see cref="CompilerOptions"/>, build a
/// compiler configured with them, and compile the script through the facade. The compiled
/// output is not emitted yet; the command reports success and will honor <c>--output</c> once
/// the later stages produce something to write.
/// </summary>
internal sealed class CompileCommand : Command<CompileSettings>
{
    private readonly ProjectConfiguration _configuration;
    private readonly Func<CompilerOptions, IScriptCompiler> _compilerFactory;

    public CompileCommand(
        ProjectConfiguration configuration, Func<CompilerOptions, IScriptCompiler> compilerFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(compilerFactory);
        _configuration = configuration;
        _compilerFactory = compilerFactory;
    }

    /// <inheritdoc />
    protected override int Execute(
        CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var options = _configuration.Resolve(settings.Config, ScriptDirectory(settings.Script));
        var compiler = _compilerFactory(options);
        var source = File.ReadAllText(settings.Script);
        compiler.Compile(source);

        // TODO(compiler): emit the compiled output, honoring --output, once the later
        // stages produce a serializable result.
        return ExitCodes.Success;
    }

    private static string ScriptDirectory(string script) =>
        Path.GetDirectoryName(Path.GetFullPath(script))!;
}
