using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds a minimal <see cref="SemanticModel"/> for tests that need one without running the
/// analyzer — an empty model bound to a given desugared tree.
/// </summary>
internal static class SemanticModelFactory
{
    public static SemanticModel Minimal(DesugaredScriptDocument desugared) =>
        new(
            desugared,
            new SpeakerTable(
                new Dictionary<string, SpeakerSymbol>(),
                new Dictionary<string, SpeakerSymbol>(),
                SpeakerSymbol.Anonymous()),
            Scene.Root(),
            new AnchorTable(),
            new JumpResolutionTable(new Dictionary<Jump, JumpResolution>()));
}
