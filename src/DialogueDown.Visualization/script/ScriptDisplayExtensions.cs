using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization;

/// <summary>
/// Ergonomic entry point for the Dialogue stage: turns a transpiled
/// <see cref="ScriptDocument"/> into a display graph without naming the projection. The
/// <paramref name="source"/> is the original script text, so each node can carry the
/// snippet it was produced from.
/// </summary>
internal static class ScriptDisplayExtensions
{
    public static DisplayGraph ToDisplayGraph(this ScriptDocument document, string source) =>
        GraphWalk.Walk<object>(document, new DialogueAstProjection(source));
}
