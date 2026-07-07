namespace DialogueDown.Visualization;

/// <summary>
/// One node prepared for display: a stable <see cref="Id"/> unique within its
/// graph, a short <see cref="Label"/>, and optional <see cref="Attributes"/>.
/// Renderer-agnostic — every output format is built from this.
/// </summary>
public sealed record DisplayNode(
    string Id,
    string Label,
    IReadOnlyList<DisplayAttribute> Attributes);
