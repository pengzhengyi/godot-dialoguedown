using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The default <see cref="ISemanticAnalyzer"/>: it runs the analysis sub-passes in dependency
/// order — index the tree, build the scene tree and anchors and the speaker table, then resolve
/// jumps and validate reserved tags against those tables — and assembles their outputs into a
/// <see cref="SemanticModel"/>. Each sub-pass is a pure function of its inputs; the analyzer
/// only wires their order.
/// </summary>
internal sealed class SemanticAnalyzer : ISemanticAnalyzer
{
    public SemanticModel Analyze(DesugaredScriptDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);

        var index = DialogueTreeIndex.Build(document);

        var (sceneRoot, anchors) = SceneBuilder.Build(document);
        var speakers = SpeakerBinder.Bind(index.OfType<Speaker>());

        var jumps = JumpResolver.Resolve(index.OfType<Jump>(), anchors);
        TagValidator.Validate(index.OfType<ReservedTag>());

        // TODO(diagnostics): source is validated but not yet read — analysis works off the tree
        // and the spans it carries. Thread source (and a DiagnosticBag) in when the diagnostics
        // phase replaces the sub-passes' throw sites with collected, source-anchored reports.
        return new SemanticModel(document, speakers, sceneRoot, anchors, jumps);
    }
}
