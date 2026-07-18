using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The default <see cref="ISemanticAnalyzer"/>: it runs the analysis sub-passes in dependency
/// order — index the tree, build the scene tree and anchors and the speaker table, then resolve
/// jumps and validate reserved tags against those tables — and assembles their outputs into a
/// <see cref="SemanticModel"/>. Each sub-pass is a pure function of its inputs; the analyzer
/// only wires their order and seeds the speaker binder's configured layer from its options.
/// </summary>
internal sealed class SemanticAnalyzer : ISemanticAnalyzer
{
    private readonly ISemanticAnalyzerOptions _options;

    public SemanticAnalyzer(ISemanticAnalyzerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public SemanticModel Analyze(DesugaredScriptDocument document, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        var index = DialogueTreeIndex.Build(document);

        var (sceneRoot, anchors) = SceneBuilder.Build(document);
        var configured = _options.ConfiguredSpeakers.Select(ConfiguredSpeakerBuilder.ToDeclaration);
        var speakers = SpeakerBinder.Bind(configured, index.OfType<Speaker>());

        var jumps = JumpResolver.Resolve(index.OfType<Jump>(), anchors, context.Diagnostics);
        TagValidator.Validate(index.OfType<ReservedTag>(), context.Diagnostics);

        // TODO(diagnostics): the context is validated but not yet read — analysis works off the
        // tree and the spans it carries. Report source-anchored diagnostics into
        // context.Diagnostics when the producers replace the sub-passes' throw sites.
        return new SemanticModel(document, speakers, sceneRoot, anchors, jumps);
    }
}
