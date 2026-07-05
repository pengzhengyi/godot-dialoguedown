using DialogueDown.Markdown;
using Ast = DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class ListBlockTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_ExposesOrderedItemsAndSpan_AndIsBlock(bool isOrdered)
    {
        var items = new[] { Ast.ListItem() };
        var span = Ast.Span();

        var list = new ListBlock(isOrdered, items, span);

        Assert.Equal(isOrdered, list.IsOrdered);
        Assert.Same(items, list.Items);
        Assert.Equal(span, list.Span);
        Assert.IsAssignableFrom<MarkdownBlock>(list);
    }
}
