using MarkdigDocument = Markdig.Syntax.MarkdownDocument;

namespace DialogueSystem.Markdown;

/// <summary>
/// Converts a Markdig syntax tree into our own Markdown AST. Keeping this
/// translation in one place is what isolates the rest of the compiler from the
/// Markdig library.
/// </summary>
internal sealed class MarkdownAstMapper
{
    public MarkdownDocument Map(MarkdigDocument document)
    {
        var blocks = new List<MarkdownBlock>();
        foreach (var block in document)
        {
            blocks.Add(MapBlock(block));
        }

        return new MarkdownDocument(blocks);
    }

    private static MarkdownBlock MapBlock(Markdig.Syntax.Block block) =>
        throw new NotSupportedException(
            $"Markdown block '{block.GetType().Name}' is not yet supported.");
}
