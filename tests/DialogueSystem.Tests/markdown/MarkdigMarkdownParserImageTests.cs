using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserImageTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_Image_ProducesImageInlineWithSourceAndAlt()
    {
        var document = Parser.Parse("![Play tennis](tennis.png)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Equal("tennis.png", image.Source);
        Assert.Equal("Play tennis", image.AltText);
    }

    [Fact]
    public void Parse_ImageWithEmptySource_IsAccepted()
    {
        // Syntactically valid; a missing image source is rejected later, not here.
        var document = Parser.Parse("![alt]()");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Empty(image.Source);
        Assert.Equal("alt", image.AltText);
    }

    [Fact]
    public void Parse_ImageWithEmptyAlt_IsAccepted()
    {
        // An empty alt text is valid; a presentation layer may still render the image.
        var document = Parser.Parse("![](tennis.png)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Equal("tennis.png", image.Source);
        Assert.Empty(image.AltText);
    }

    [Fact]
    public void Parse_ImageAltWithCodeSpan_FlattensToRawText()
    {
        // A formatted alt text (here a code span) is flattened to raw text, mirroring links.
        var document = Parser.Parse("![a `b` c](x.png)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Equal("x.png", image.Source);
        Assert.Equal("a `b` c", image.AltText);
    }

    [Fact]
    public void Parse_ImageAmongText_KeepsSurroundingText()
    {
        // An image can sit inline with speech (e.g. a portrait or emoji mid-chat),
        // so the surrounding text stays its own runs.
        var document = Parser.Parse("see ![alt](x.png) end");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "see "),
            inline =>
            {
                var image = Assert.IsType<ImageInline>(inline);
                Assert.Equal("x.png", image.Source);
                Assert.Equal("alt", image.AltText);
            },
            inline => AssertText(inline, " end"));
    }
}
