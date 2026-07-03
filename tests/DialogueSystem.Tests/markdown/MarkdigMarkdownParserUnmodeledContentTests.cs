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
    [InlineData("![alt](image.png)")]
    [InlineData("<https://example.com>")]
    public void Parse_UnmodeledInline_FlattensToRawText(string source)
    {
        // Images and autolinks are not modeled; they survive as their exact source
        // text so nothing spoken is lost.
        var document = Parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, source);
    }

    [Fact]
    public void Parse_ImageAmongText_FlattensImageAndKeepsSurroundingText()
    {
        // Flattening is local: only the unmodeled image becomes raw text; the
        // surrounding literals stay their own text runs.
        var document = Parser.Parse("see ![alt](x.png) end");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "see "),
            inline => AssertText(inline, "![alt](x.png)"),
            inline => AssertText(inline, " end"));
    }
}
