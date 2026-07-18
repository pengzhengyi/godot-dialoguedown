using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Transpiles a Markdown AST into the Dialogue AST. This is the seam the rest of the
/// compiler depends on, so the block-walking implementation can change without touching
/// downstream code, mirroring the front-end's <see cref="IMarkdownParser"/>.
/// </summary>
internal interface IScriptTranspiler
{
    /// <summary>
    /// Transpiles <paramref name="document"/> into a <see cref="ScriptDocument"/>.
    /// <paramref name="context"/> carries the source a diagnostic is anchored to and the sink to
    /// report into; it is validated here, though the transpile itself reads text and spans from
    /// the Markdown AST.
    /// </summary>
    ScriptDocument Transpile(MarkdownDocument document, DiagnosticsContext context);
}
