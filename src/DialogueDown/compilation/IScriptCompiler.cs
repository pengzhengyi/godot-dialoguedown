namespace DialogueDown.Compilation;

/// <summary>
/// Compiles a DialogueDown script's source into a <see cref="CompilationResult"/> by
/// running the compiler stages in order. This is the single public entry point that
/// embedders and tools depend on; a ready instance comes from the default factory or from
/// an <c>AddDialogueDown</c> container registration.
/// </summary>
public interface IScriptCompiler
{
    /// <summary>Compiles <paramref name="source"/> — the script's text.</summary>
    CompilationResult Compile(string source);
}
