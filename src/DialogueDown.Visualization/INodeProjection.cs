namespace DialogueDown.Visualization;

/// <summary>
/// The unified seam for presenting one intermediate representation (IR) family.
/// An implementation names the stage (<see cref="Title"/>), describes any node
/// (<see cref="Describe"/>), and yields a node's out-neighbours
/// (<see cref="Neighbours"/>). The generic <see cref="GraphWalk"/> supplies the
/// cycle-safe traversal, so a projection never builds a graph itself. Adding a
/// stage is one small projection, not a bespoke graph-building routine.
/// </summary>
/// <typeparam name="TNode">
/// The IR node type. A heterogeneous AST whose nodes share no common base uses
/// <see cref="object"/> and pattern-matches inside the projection.
/// </typeparam>
public interface INodeProjection<TNode>
    where TNode : class
{
    /// <summary>The stage title, for example <c>"Markdown AST"</c>.</summary>
    string Title { get; }

    /// <summary>Describes one node: its label and any extra attributes.</summary>
    NodeDescription Describe(TNode node);

    /// <summary>The node's out-neighbours, in the order they should be shown.</summary>
    IEnumerable<TNode> Neighbours(TNode node);
}
