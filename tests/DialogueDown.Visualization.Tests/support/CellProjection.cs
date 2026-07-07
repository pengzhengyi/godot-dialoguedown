namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// A configurable projection over <see cref="Cell"/>. Edges and attributes live
/// in side tables keyed by reference identity, so a test can wire arbitrary
/// trees, shared nodes, and cycles — including value-equal siblings — without the
/// node type carrying its own structure.
/// </summary>
internal sealed class CellProjection : INodeProjection<Cell>
{
    private readonly Dictionary<Cell, IReadOnlyList<Cell>> _neighbours =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, IReadOnlyList<DisplayAttribute>> _attributes =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, string> _sources =
        new(ReferenceEqualityComparer.Instance);

    public string Title { get; init; } = "Cells";

    public CellProjection Link(Cell from, params Cell[] to)
    {
        _neighbours[from] = to;
        return this;
    }

    public CellProjection WithAttributes(Cell node, params DisplayAttribute[] attributes)
    {
        _attributes[node] = attributes;
        return this;
    }

    public CellProjection WithSource(Cell node, string source)
    {
        _sources[node] = source;
        return this;
    }

    public NodeDescription Describe(Cell node) =>
        new(
            node.Name,
            _attributes.TryGetValue(node, out var attributes) ? attributes : null,
            _sources.TryGetValue(node, out var source) ? source : null);

    public IEnumerable<Cell> Neighbours(Cell node) =>
        _neighbours.TryGetValue(node, out var neighbours) ? neighbours : [];
}
