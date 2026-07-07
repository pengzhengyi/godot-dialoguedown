namespace DialogueDown.Visualization;

/// <summary>
/// What a projection reports about a single IR node: a short <see cref="Label"/>
/// and any <see cref="Attributes"/> (extra detail such as a span or kind). The
/// walk turns this into a <see cref="DisplayNode"/> by adding an identity.
/// </summary>
public sealed record NodeDescription
{
    public NodeDescription(string label, IReadOnlyList<DisplayAttribute>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);
        Label = label;
        Attributes = attributes ?? [];
    }

    public string Label { get; }

    public IReadOnlyList<DisplayAttribute> Attributes { get; }
}
