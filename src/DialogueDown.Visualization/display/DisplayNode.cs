namespace DialogueDown.Visualization;

/// <summary>
/// One node prepared for display: a stable <see cref="Id"/> unique within its
/// graph, a short <see cref="Label"/>, optional <see cref="Attributes"/>, the
/// optional original <see cref="Source"/> text it was produced from, an optional
/// semantic <see cref="Category"/> (a stable group name that drives color), an
/// optional <see cref="EntityKey"/> tying the node to a cross-linked entity (a scene
/// shared with the semantic tab's tables), and an optional <see cref="TypeName"/> naming
/// the node's kind for the legend when its label carries content rather than a type.
/// Renderer-agnostic — every output format is built from this.
/// </summary>
public sealed record DisplayNode(
    string Id,
    string Label,
    IReadOnlyList<DisplayAttribute> Attributes,
    string? Source = null,
    string? Category = null,
    string? EntityKey = null,
    string? TypeName = null);
