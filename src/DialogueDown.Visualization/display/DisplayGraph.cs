namespace DialogueDown.Visualization;

/// <summary>
/// A titled diagram for one compiler stage: a short <see cref="Description"/> of
/// what it shows, its <see cref="Nodes"/>, and the <see cref="Edges"/> between
/// them. A tree is the acyclic, single-parent case; a stage with shared nodes or
/// cycles shows those as reference edges.
/// </summary>
public sealed record DisplayGraph(
    string Title,
    string Description,
    IReadOnlyList<DisplayNode> Nodes,
    IReadOnlyList<DisplayEdge> Edges);
