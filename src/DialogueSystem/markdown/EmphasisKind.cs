namespace DialogueSystem.Markdown;

/// <summary>
/// Whether emphasized text is italic (a single <c>*</c>/<c>_</c> delimiter) or
/// bold (a double delimiter). Bold-italic is represented by nesting one inside
/// the other, so no combined value is needed.
/// </summary>
internal enum EmphasisKind
{
    Italic,
    Bold,
}
