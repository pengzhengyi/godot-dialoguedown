namespace DialogueDown.Cli.Compilation;

/// <summary>
/// The compiled form of a script. A placeholder for now: the transpiler component
/// enriches it with the Dialogue AST, stages, and diagnostics; the visualization
/// component renders from it.
/// </summary>
internal sealed record CompilationResult(string Source);
