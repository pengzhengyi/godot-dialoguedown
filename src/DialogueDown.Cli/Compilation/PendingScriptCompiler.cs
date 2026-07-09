namespace DialogueDown.Cli.Compilation;

/// <summary>
/// The skeleton compiler: compilation is not built yet, so it fails clearly rather
/// than pretending to succeed. The transpiler component replaces this registration
/// with the real implementation, and the commands light up with no change to their
/// bodies.
/// </summary>
internal sealed class PendingScriptCompiler : IScriptCompiler
{
    /// <inheritdoc />
    public CompilationResult Compile(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        throw new NotImplementedException(
            "Script compilation is not implemented yet — the transpiler will provide it.");
    }
}
