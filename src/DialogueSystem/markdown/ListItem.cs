namespace DialogueSystem.Markdown;

/// <summary>
/// One entry in a list. It holds its own blocks, so a list item can contain a
/// paragraph and a nested list — which is how nested choices are represented.
/// A list item is not itself a block.
/// </summary>
internal sealed record ListItem(IReadOnlyList<MarkdownBlock> Blocks, SourceSpan Span);
