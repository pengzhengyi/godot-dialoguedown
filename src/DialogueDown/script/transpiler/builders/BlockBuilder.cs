using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Walks the Markdown block tree into the Dialogue AST skeleton. It is a faithful,
/// local tokenizer: a heading becomes a flat <see cref="SceneHeading"/> marker, a
/// paragraph becomes one or more <see cref="Line"/>s, and a list becomes
/// <see cref="Choices"/>. Composition across siblings — grouping headings into scenes,
/// assembling jumps — is deferred to later stages. One shared, recursive
/// <see cref="Build"/> serves both the document body and each choice body.
/// </summary>
internal sealed class BlockBuilder(InlineBuilder inlineBuilder)
{
    public IReadOnlyList<Block> Build(IReadOnlyList<MarkdownBlock> blocks)
    {
        var result = new List<Block>();
        foreach (var block in blocks)
        {
            Append(block, result);
        }

        return result;
    }

    private void Append(MarkdownBlock block, List<Block> blocks)
    {
        switch (block)
        {
            case Heading heading:
                blocks.Add(new SceneHeading(
                    inlineBuilder.Build(heading.Inlines), heading.Level, heading.Span));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(block), block.GetType().Name,
                    $"Cannot transpile a block of kind '{block.GetType().Name}' because it "
                    + "is not one of the supported block types.");
        }
    }
}
