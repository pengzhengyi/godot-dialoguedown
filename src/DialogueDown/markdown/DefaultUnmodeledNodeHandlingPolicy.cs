namespace DialogueDown.Markdown;

/// <summary>
/// The default handling policy: drop authoring aids that are not speech (code
/// blocks, thematic breaks, tables) and keep everything else as raw text.
/// </summary>
internal sealed class DefaultUnmodeledNodeHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    private DefaultUnmodeledNodeHandlingPolicy()
    {
    }

    public static DefaultUnmodeledNodeHandlingPolicy Instance { get; } = new();

    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => kind switch
    {
        UnmodeledNodeKind.CodeBlock
            or UnmodeledNodeKind.ThematicBreak
            or UnmodeledNodeKind.Table => UnmodeledNodeHandling.Ignore,
        _ => UnmodeledNodeHandling.AsRawText,
    };
}
