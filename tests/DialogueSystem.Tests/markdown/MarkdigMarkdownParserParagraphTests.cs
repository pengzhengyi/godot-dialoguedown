using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserParagraphTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_PlainParagraph_ProducesParagraphWithRawText()
    {
        var document = Parser.Parse("Hello, Bob!");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Hello, Bob!");
    }
}
