using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserUnmodeledContentTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("> quote")]
    [InlineData("---")]
    public void Parse_UnmodeledBlock_FlattensToRawTextParagraph(string source)
    {
        // Blockquotes, thematic breaks, and the like are not modeled; they survive
        // as a paragraph of their exact source text rather than throwing.
        var document = Parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, source);
    }

    [Theory]
    [InlineData("<https://example.com>")]
    [InlineData("<mailto:alice@example.com>")]
    public void Parse_UnmodeledInline_FlattensToRawText(string source)
    {
        // Autolinks are not modeled; they survive as their exact source text so
        // nothing spoken is lost.
        var document = Parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, source);
    }
}
