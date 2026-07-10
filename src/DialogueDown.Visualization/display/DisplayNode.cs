namespace DialogueDown.Visualization;

/// <summary>
/// One node prepared for display: a stable <see cref="Id"/> unique within its
/// graph, a short <see cref="Label"/>, optional <see cref="Attributes"/>, the
/// optional original <see cref="Source"/> text it was produced from, and an
/// optional semantic <see cref="Category"/> (a stable group name that drives
/// color). Renderer-agnostic — every output format is built from this.
/// </summary>
public sealed record DisplayNode(
    string Id,
    string Label,
    IReadOnlyList<DisplayAttribute> Attributes,
    string? Source = null,
    string? Category = null);
