namespace DialogueDown.Script.Ast;

/// <summary>
/// How some speech text is styled: italic, bold, or strikethrough. This is the
/// Dialogue-side style, kept separate from the Markdown <c>EmphasisKind</c> so the AST
/// does not depend on Markdown. Bold-italic is one style nested inside another, so no
/// combined value is needed.
/// </summary>
internal enum SpeechStyle
{
    Italic,
    Bold,
    Strikethrough,
}
