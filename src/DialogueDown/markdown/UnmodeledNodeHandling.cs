namespace DialogueDown.Markdown;

/// <summary>
/// What the front-end does with an unmodeled Markdown construct: keep it as raw
/// speech text, or drop it entirely.
/// </summary>
internal enum UnmodeledNodeHandling
{
    /// <summary>Keep the construct as literal speech text (the default).</summary>
    AsRawText,

    /// <summary>Drop the construct so it never reaches speech, like a comment.</summary>
    Ignore,
}
