using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Analyzes a desugared Dialogue AST into a <see cref="SemanticModel"/> — resolving speakers,
/// scenes and anchors, and jumps, and validating references. This is the seam the compiler
/// facade and the future graph builder depend on, mirroring <c>IScriptDesugarer</c>.
/// </summary>
internal interface ISemanticAnalyzer
{
    /// <summary>
    /// Analyzes <paramref name="document"/> into a <see cref="SemanticModel"/>.
    /// <paramref name="source"/> is the original script text; it anchors future diagnostics and
    /// is validated here, though analysis itself reads only the tree and the spans it carries.
    /// </summary>
    SemanticModel Analyze(DesugaredScriptDocument document, string source);
}
