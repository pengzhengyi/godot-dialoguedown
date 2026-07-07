namespace DialogueDown.Visualization;

/// <summary>
/// A directed link between two display nodes, identified by their ids and tagged
/// with a <see cref="Kind"/> — a normal child edge, or a reference back to an
/// already-seen node.
/// </summary>
public sealed record DisplayEdge(string FromId, string ToId, DisplayEdgeKind Kind);
