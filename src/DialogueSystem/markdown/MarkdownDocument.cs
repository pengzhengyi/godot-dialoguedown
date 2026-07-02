namespace DialogueSystem.Markdown;

/// <summary>
/// The whole parsed document: an ordered list of top-level blocks. This is the
/// root of the Markdown AST and the result of parsing a script.
/// </summary>
internal sealed record MarkdownDocument(IReadOnlyList<MarkdownBlock> Blocks);
