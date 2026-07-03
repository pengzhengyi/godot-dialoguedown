using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserCommentTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_InlineComment_IsStrippedFromSpeech()
    {
        var document = Parser.Parse("Alice: Hello! <!-- warm -->");

        // The comment is dropped; the surrounding text stays raw (trailing space
        // included), to be trimmed later by the dialogue layer.
        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Alice: Hello! ");
    }

    [Fact]
    public void Parse_BlockComment_IsDiscarded()
    {
        var document = Parser.Parse("<!-- just a note -->");

        Assert.Empty(document.Blocks);
    }

    [Fact]
    public void Parse_BlockCommentBetweenContent_IsDiscarded()
    {
        var document = Parser.Parse("<!-- note -->\nBob: Hey");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Bob: Hey");
    }

    [Fact]
    public void Parse_EndMarkerOnly_IsLiteralText()
    {
        // A lone "-->" is not a comment; it stays as text.
        var document = Parser.Parse("-->");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "-->");
    }

    [Fact]
    public void Parse_UnclosedInlineComment_StaysLiteral()
    {
        // An inline comment must be well-formed to be dropped; an unclosed one is text.
        var document = Parser.Parse("x <!-- unclosed");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "x <!-- unclosed");
    }

    [Fact]
    public void Parse_ThreeDashComment_IsDiscarded()
    {
        // "<!---" still opens a comment (content may start with '-'), so it is dropped.
        var document = Parser.Parse("<!--- weird --->");

        Assert.Empty(document.Blocks);
    }
}
