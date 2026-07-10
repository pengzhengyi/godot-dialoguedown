using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Desugars a transpiled Dialogue AST, applying the local normalizations the transpiler
/// deferred (jump assembly, default-speaker fill). This is the seam downstream stages
/// depend on, mirroring <c>IScriptTranspiler</c>.
/// </summary>
internal interface IScriptDesugarer
{
    /// <summary>
    /// Desugars <paramref name="document"/> into a <see cref="DesugaredScriptDocument"/>.
    /// <paramref name="source"/> is the original script text; it anchors future diagnostics
    /// and is validated here, though desugaring itself reads only the tree.
    /// </summary>
    DesugaredScriptDocument Desugar(ScriptDocument document, string source);
}
