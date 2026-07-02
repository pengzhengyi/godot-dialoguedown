using DialogueSystem.Markdown;
using Ast = DialogueSystem.Tests.Support.MarkdownAstFactory;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdownDocumentTests
{
    [Fact]
    public void Constructor_ExposesBlocks()
    {
        var blocks = new MarkdownBlock[] { Ast.Paragraph(Ast.Text("Hi")) };

        var document = new MarkdownDocument(blocks);

        Assert.Same(blocks, document.Blocks);
    }

    [Fact]
    public void Constructor_EmptyBlocks_IsAllowed()
    {
        var document = new MarkdownDocument([]);

        Assert.Empty(document.Blocks);
    }
}
