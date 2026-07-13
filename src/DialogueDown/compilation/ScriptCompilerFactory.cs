using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;

namespace DialogueDown.Compilation;

/// <summary>
/// The container-free composition root for the default <see cref="IScriptCompiler"/>: it
/// wires the standard stages — the Markdig-based parser, the default transpiler, the
/// desugarer, and the semantic analyzer — into a ready compiler in one call, for callers that
/// do not run a dependency injection container. Container callers use the <c>AddDialogueDown</c>
/// registration instead; both build the same graph.
/// </summary>
public static class ScriptCompilerFactory
{
    /// <summary>Creates the default compiler with its standard stage graph.</summary>
    public static IScriptCompiler CreateDefault() =>
        new ScriptCompiler(
            new MarkdigMarkdownParser(),
            ScriptTranspilerFactory.CreateDefault(),
            new ScriptDesugarer(),
            new SemanticAnalyzer());
}
