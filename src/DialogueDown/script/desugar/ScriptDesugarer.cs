using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// The default <see cref="IScriptDesugarer"/>: it runs the <see cref="Desugarer"/> rewrite
/// over the document and wraps the result as a <see cref="DesugaredScriptDocument"/>.
/// </summary>
internal sealed class ScriptDesugarer : IScriptDesugarer
{
    private readonly Desugarer _desugarer = new();

    public DesugaredScriptDocument Desugar(ScriptDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);

        // TODO(diagnostics): source is validated but not yet read — desugaring works off
        // the tree and the spans it already carries. Thread source into warning reporting
        // when the diagnostics phase lands (e.g. a dangling arrow or multiple jumps).
        return new DesugaredScriptDocument(_desugarer.Rewrite(document));
    }
}
