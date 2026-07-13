namespace DialogueDown.Visualization;

/// <summary>
/// A titled diagram for one compiler stage: a short <see cref="Description"/> of
/// what it shows, its <see cref="Nodes"/>, the <see cref="Edges"/> between them, and
/// optional <see cref="Tables"/> shown beside the graph (the semantic tab's speaker,
/// anchor, and jump-resolution tables; null for a plain graph stage). A tree is the
/// acyclic, single-parent case; a stage with shared nodes or cycles shows those as
/// reference edges.
/// </summary>
public sealed record DisplayGraph(
    string Title,
    string Description,
    IReadOnlyList<DisplayNode> Nodes,
    IReadOnlyList<DisplayEdge> Edges,
    IReadOnlyList<SemanticTable>? Tables = null);
