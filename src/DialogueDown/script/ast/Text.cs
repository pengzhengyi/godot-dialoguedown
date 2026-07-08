using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Plain words in a line's speech, kept exactly as the author wrote them. It always
/// has at least one character.
/// </summary>
internal sealed record Text : SpeechFragment
{
    public Text(string content, SourceSpan span)
        : base(span)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        Content = content;
    }

    public string Content { get; }
}
