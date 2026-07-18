using DialogueDown.Diagnostics;
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
    /// <paramref name="context"/> carries the source future diagnostics anchor to and the sink to
    /// report into; it is validated here, though analysis itself reads only the tree and spans.
    /// </summary>
    SemanticModel Analyze(DesugaredScriptDocument document, DiagnosticsContext context);
}
