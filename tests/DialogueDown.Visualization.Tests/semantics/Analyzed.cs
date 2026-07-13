using DialogueDown.Compilation;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Tests.Semantics;

/// <summary>
/// Builds a real <see cref="SemanticModel"/> from script text through the default compiler,
/// so a projection test reads the same model the report shows.
/// </summary>
internal static class Analyzed
{
    public static SemanticModel Model(string source) =>
        ScriptCompilerFactory.CreateDefault().Compile(source).Semantics;
}
