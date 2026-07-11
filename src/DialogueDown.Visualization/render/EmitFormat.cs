namespace DialogueDown.Visualization;

/// <summary>
/// A text output format for emitting a compiler stage's graph, for embedding
/// elsewhere: <see cref="Mermaid"/> renders inline on GitHub and in many editors;
/// <see cref="Dot"/> is Graphviz DOT for layout tools.
/// </summary>
public enum EmitFormat
{
    /// <summary>Mermaid <c>flowchart</c> text.</summary>
    Mermaid,

    /// <summary>Graphviz DOT text.</summary>
    Dot,
}
