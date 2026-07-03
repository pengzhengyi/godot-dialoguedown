using DialogueSystem.Markdown;
using Ast = DialogueSystem.Tests.Support.MarkdownAstFactory;

namespace DialogueSystem.Tests.Markdown;

public sealed class ParagraphTests
{
    [Fact]
    public void Constructor_ExposesInlinesAndSpan_AndIsBlock()
    {
        var inlines = new MarkdownInline[] { Ast.Text("Hello, Bob!") };
        var span = Ast.Span();

        var paragraph = new Paragraph(inlines, span);

        Assert.Same(inlines, paragraph.Inlines);
        Assert.Equal(span, paragraph.Span);
        Assert.IsAssignableFrom<MarkdownBlock>(paragraph);
    }
}
