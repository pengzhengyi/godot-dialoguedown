using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserCustomPolicyTests
{
    [Fact]
    public void CustomPolicy_KeepingTablesAsRawText_FlattensTableToText()
    {
        // Override the default (which ignores tables) to keep the table as raw text.
        var parser = new MarkdigMarkdownParser(
            TestUnmodeledNodePolicy.Default.Keep(UnmodeledNodeKind.Table));

        var document = parser.Parse(
            """
            | a | b |
            | --- | --- |
            | x | y |
            """);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var text = Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
        Assert.StartsWith("| a | b |", text.Text);
    }

    [Fact]
    public void CustomPolicy_IgnoringAutolinks_DropsAutolinkFromSpeech()
    {
        // Override the default (which keeps autolinks) to drop them.
        var parser = new MarkdigMarkdownParser(
            TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.Autolink));

        var document = parser.Parse("see <https://example.com> end");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertAllText(paragraph.Inlines, "see  end");
    }
}
