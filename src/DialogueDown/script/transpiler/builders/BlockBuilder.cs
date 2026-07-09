using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Walks the Markdown block tree into the Dialogue AST skeleton. It orchestrates: a
/// heading becomes a flat <see cref="SceneHeading"/> marker, a paragraph is handed to the
/// <see cref="LineBuilder"/>, and a list becomes <see cref="Choices"/>. It is a faithful,
/// local tokenizer — composition across siblings, such as grouping headings into scenes,
/// is deferred to later stages. One shared, recursive <see cref="Build"/> serves both the
/// document body and each choice body.
/// </summary>
internal sealed class BlockBuilder(InlineBuilder inlineBuilder, LineBuilder lineBuilder)
{
    public IReadOnlyList<ScriptBlock> Build(IReadOnlyList<MarkdownBlock> blocks)
    {
        var result = new List<ScriptBlock>();
        foreach (var block in blocks)
        {
            Append(block, result);
        }

        return result;
    }

    private void Append(MarkdownBlock block, List<ScriptBlock> blocks)
    {
        switch (block)
        {
            case Heading heading:
                blocks.Add(new SceneHeading(
                    inlineBuilder.Build(heading.Inlines), heading.Level, heading.Span));
                break;
            case Paragraph paragraph:
                blocks.Add(lineBuilder.Build(paragraph.Inlines));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(block), block.GetType().Name,
                    $"Cannot transpile a block of kind '{block.GetType().Name}' because it "
                    + "is not one of the supported block types.");
        }
    }
}
