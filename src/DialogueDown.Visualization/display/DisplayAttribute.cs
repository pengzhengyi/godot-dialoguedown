namespace DialogueDown.Visualization;

/// <summary>
/// A named extra shown alongside a display node's label — for example a source
/// span, a kind, or a link target. Purely informational and renderer-agnostic.
/// </summary>
public sealed record DisplayAttribute(string Name, string Value);
