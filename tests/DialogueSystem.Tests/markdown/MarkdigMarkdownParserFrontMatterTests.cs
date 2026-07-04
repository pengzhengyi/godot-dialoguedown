using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserFrontMatterTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_FrontMatter_IsDiscarded_ContentPreserved()
    {
        var document = Parser.Parse(
            """
            ---
            title: Scene 1
            tags: [intro]
            ---
            Alice: Hello
            """);

        // The leading front matter never becomes speech; the content after it stays.
        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Alice: Hello");
    }

    [Fact]
    public void Parse_FrontMatterOnly_ProducesNoBlocks()
    {
        var document = Parser.Parse(
            """
            ---
            title: Scene 1
            tags: [intro]
            ---
            """);

        Assert.Empty(document.Blocks);
    }

    [Fact]
    public void Parse_DashesAfterContent_AreThematicBreak_NotFrontMatter()
    {
        // A "---" that follows content is an ordinary thematic break, handled by the
        // policy — not front matter (which only matches at the document start). A
        // policy that keeps thematic breaks proves the "---" survives as raw text,
        // whereas front matter would be discarded regardless of policy.
        var parser = new MarkdigMarkdownParser(
            TestUnmodeledNodePolicy.Default.Keep(UnmodeledNodeKind.ThematicBreak));

        var document = parser.Parse(
            """
            Alice: Hi

            ---

            Bob: Bye
            """);

        Assert.Equal(3, document.Blocks.Count);
        var divider = Assert.IsType<Paragraph>(document.Blocks[1]);
        var text = Assert.IsType<TextInline>(Assert.Single(divider.Inlines));
        Assert.StartsWith("---", text.Text);
    }
}
