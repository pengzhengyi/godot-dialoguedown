using DialogueSystem.Markdown;
using Ast = DialogueSystem.Tests.Support.MarkdownAstFactory;

namespace DialogueSystem.Tests.Markdown;

public sealed class HeadingTests
{
    [Fact]
    public void Constructor_ExposesLevelInlinesAndSpan_AndIsBlock()
    {
        var inlines = new MarkdownInline[] { Ast.Text("Greetings") };
        var span = Ast.Span();

        var heading = new Heading(2, inlines, span);

        Assert.Equal(2, heading.Level);
        Assert.Same(inlines, heading.Inlines);
        Assert.Equal(span, heading.Span);
        Assert.IsAssignableFrom<MarkdownBlock>(heading);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Constructor_LevelAtBoundary_IsAccepted(int level)
    {
        var heading = new Heading(level, [], Ast.Span());

        Assert.Equal(level, heading.Level);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void Constructor_LevelOutOfRange_Throws(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Heading(level, [], Ast.Span()));
    }
}
