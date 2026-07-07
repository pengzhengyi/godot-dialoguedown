namespace DialogueDown.Visualization;

/// <summary>
/// A titled diagram for one compiler stage: its <see cref="Nodes"/> and the
/// <see cref="Edges"/> between them. A tree is the acyclic, single-parent case;
/// a stage with shared nodes or cycles shows those as reference edges.
/// </summary>
public sealed record DisplayGraph(
    string Title,
    IReadOnlyList<DisplayNode> Nodes,
    IReadOnlyList<DisplayEdge> Edges);
