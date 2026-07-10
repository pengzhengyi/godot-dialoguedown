using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// A <see cref="ScriptDocument"/> that has been desugared — a distinct pipeline-stage type
/// so a later stage (semantic analysis) can take only a desugared tree, and Desugar cannot
/// be skipped. It is a thin wrapper that surfaces the document's <see cref="Body"/>. The
/// marker attests ordering (Desugar ran); the post-desugar invariants — no
/// <c>JumpIndicator</c> survives, no line lacks a speaker — are upheld by the
/// implementation, not the type.
/// </summary>
internal sealed record DesugaredScriptDocument(ScriptDocument Document)
{
    public IReadOnlyList<ScriptBlock> Body => Document.Body;
}
