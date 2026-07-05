using DialogueDown.Markdown;
using Ast = DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class TextInlineTests
{
    [Fact]
    public void Constructor_ExposesTextAndSpan_AndIsInline()
    {
        var span = Ast.Span();

        var inline = new TextInline("hello", span);

        Assert.Equal("hello", inline.Text);
        Assert.Equal(span, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }

    [Fact]
    public void Constructor_NullText_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TextInline(null!, Ast.Span()));
    }

    [Fact]
    public void Constructor_EmptyText_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TextInline(string.Empty, Ast.Span()));
    }

    [Fact]
    public void Equality_SameTextAndSpan_AreEqual()
    {
        var span = Ast.Span();

        Assert.Equal(new TextInline("hi", span), new TextInline("hi", span));
    }
}
