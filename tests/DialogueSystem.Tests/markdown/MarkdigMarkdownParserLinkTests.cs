using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserLinkTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_Link_ProducesLinkInlineWithTargetAndLabel()
    {
        var document = Parser.Parse("[Play tennis](#play-tennis)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#play-tennis", link.Target);
        Assert.Equal("Play tennis", link.Label);
    }

    [Fact]
    public void Parse_LinkLabelWithEmphasisMarkers_FlattensToRawText()
    {
        // Emphasis is disabled, so markers in a label stay literal in the flattened text.
        var document = Parser.Parse("[Play *tennis*](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Equal("Play *tennis*", link.Label);
    }

    [Fact]
    public void Parse_LinkWithEmptyTarget_IsAccepted()
    {
        // Syntactically valid; an empty jump target is rejected later, not here.
        var document = Parser.Parse("[label]()");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal(string.Empty, link.Target);
        Assert.Equal("label", link.Label);
    }

    [Fact]
    public void Parse_LinkWithFormattedLabel_Throws()
    {
        // Jump labels are expected to be plain text; a formatted label (here a
        // code span inside the label) is not supported.
        Assert.Throws<NotSupportedException>(() => Parser.Parse("[a `b` c](#x)"));
    }
}
