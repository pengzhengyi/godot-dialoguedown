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
        Assert.Empty(link.Target);
        Assert.Equal("label", link.Label);
    }

    [Fact]
    public void Parse_LinkWithEmptyLabel_IsAccepted()
    {
        // An empty label is syntactically valid; whether a jump needs display text
        // is decided downstream, not here.
        var document = Parser.Parse("[](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Empty(link.Label);
    }

    [Fact]
    public void Parse_LinkLabelWithCodeSpan_FlattensToRawText()
    {
        // A formatted label (here a code span inside it) is flattened to raw text,
        // not rejected; the label is never treated as dialogue structure.
        var document = Parser.Parse("[a `b` c](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Equal("a `b` c", link.Label);
    }
}
