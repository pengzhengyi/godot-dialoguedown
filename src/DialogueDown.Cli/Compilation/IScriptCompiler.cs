namespace DialogueDown.Cli.Compilation;

/// <summary>
/// Compiles a DialogueDown script's source into its compiled form. This is the
/// single seam through which both the <c>compile</c> and <c>visualize</c> commands
/// run compilation, so <c>visualize</c> relies on compilation rather than
/// re-implementing it. The transpiler component provides the real implementation.
/// </summary>
internal interface IScriptCompiler
{
    /// <summary>Compiles <paramref name="source"/> — the script's text.</summary>
    CompilationResult Compile(string source);
}
