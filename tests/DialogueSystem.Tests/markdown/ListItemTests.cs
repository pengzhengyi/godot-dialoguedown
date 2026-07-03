using DialogueSystem.Markdown;
using Ast = DialogueSystem.Tests.Support.MarkdownAstFactory;

namespace DialogueSystem.Tests.Markdown;

public sealed class ListItemTests
{
    [Fact]
    public void Constructor_ExposesBlocksAndSpan()
    {
        var blocks = new MarkdownBlock[] { Ast.Paragraph(Ast.Text("Really?")) };
        var span = Ast.Span();

        var item = new ListItem(blocks, span);

        Assert.Same(blocks, item.Blocks);
        Assert.Equal(span, item.Span);
    }

    [Fact]
    public void ListItem_IsNotABlock()
    {
        Assert.False(typeof(MarkdownBlock).IsAssignableFrom(typeof(ListItem)));
    }
}
