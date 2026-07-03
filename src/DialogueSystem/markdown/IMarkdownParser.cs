namespace DialogueSystem.Markdown;

/// <summary>
/// Parses a raw script string into a Markdown AST. This is the seam the rest of
/// the compiler depends on, so the Markdown library behind it can be swapped
/// without touching downstream code.
/// </summary>
internal interface IMarkdownParser
{
    MarkdownDocument Parse(string source);
}
