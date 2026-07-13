namespace DialogueDown.Visualization;

/// <summary>
/// What a projection reports about a single IR node: a short <see cref="Label"/>,
/// any <see cref="Attributes"/> (extra detail such as a span or kind), an optional
/// semantic <see cref="Category"/> (a stable group name that drives color), and
/// optionally the original <see cref="Source"/> text the node was produced from.
/// The walk turns this into a <see cref="DisplayNode"/> by adding an identity.
/// </summary>
public sealed record NodeDescription
{
    public NodeDescription(
        string label,
        IReadOnlyList<DisplayAttribute>? attributes = null,
        string? source = null,
        string? category = null,
        string? entityKey = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);
        Label = label;
        Attributes = attributes ?? [];
        Source = source;
        Category = category;
        EntityKey = entityKey;
    }

    public string Label { get; }

    public IReadOnlyList<DisplayAttribute> Attributes { get; }

    /// <summary>The original source text this node was produced from, if known.</summary>
    public string? Source { get; }

    /// <summary>
    /// A stable, cross-stage semantic group name (for example <c>"call"</c> or
    /// <c>"speech"</c>) that a renderer maps to a color. Corresponding concepts in
    /// different stages share a category, so they share a color.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// A stable key tying this node to a cross-linked entity — a scene shared with the
    /// semantic tab's tables (for example <c>"scene:the-market"</c>), or null when the
    /// node is not cross-linked. Hovering it highlights every element sharing the key.
    /// </summary>
    public string? EntityKey { get; }
}
