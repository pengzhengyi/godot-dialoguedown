using DialogueSystem.Markdown;
using Ast = DialogueSystem.Tests.Support.MarkdownAstFactory;

namespace DialogueSystem.Tests.Markdown;

public sealed class LinkInlineTests
{
    [Fact]
    public void Constructor_ExposesTargetLabelAndSpan_AndIsInline()
    {
        var span = Ast.Span();

        var inline = new LinkInline("#play-tennis", "Play tennis", span);

        Assert.Equal("#play-tennis", inline.Target);
        Assert.Equal("Play tennis", inline.Label);
        Assert.Equal(span, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }
}
