using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserLineBreakTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("line one\nline two")]
    [InlineData("line one\r\nline two")]
    public void Parse_SoftLineBreak_PreservedAsSoftBreak(string source)
    {
        // A plain newline (either line-ending style) is a soft break: the two
        // lines stay in one paragraph and the compiler later joins them.
        var document = Parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "line one"),
            inline => AssertLineBreak(inline, isHard: false),
            inline => AssertText(inline, "line two"));
    }

    [Theory]
    [InlineData("line one  \nline two")]
    [InlineData("line one\\\nline two")]
    public void Parse_HardLineBreak_PreservedAsHardBreak(string source)
    {
        // Two trailing spaces or a trailing backslash both make a hard break,
        // which the compiler later reads as the boundary between two speeches.
        var document = Parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "line one"),
            inline => AssertLineBreak(inline, isHard: true),
            inline => AssertText(inline, "line two"));
    }
}
