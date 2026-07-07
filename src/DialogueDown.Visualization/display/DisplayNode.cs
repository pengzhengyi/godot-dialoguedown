namespace DialogueDown.Visualization;

/// <summary>
/// One node prepared for display: a stable <see cref="Id"/> unique within its
/// graph, a short <see cref="Label"/>, optional <see cref="Attributes"/>, and the
/// optional original <see cref="Source"/> text it was produced from.
/// Renderer-agnostic — every output format is built from this.
/// </summary>
public sealed record DisplayNode(
    string Id,
    string Label,
    IReadOnlyList<DisplayAttribute> Attributes,
    string? Source = null);
