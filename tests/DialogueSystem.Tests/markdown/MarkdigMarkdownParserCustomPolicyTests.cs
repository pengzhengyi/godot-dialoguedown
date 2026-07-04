using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserCustomPolicyTests
{
    [Fact]
    public void CustomPolicy_KeepingTablesAsRawText_FlattensTableToText()
    {
        // Override the default (which ignores tables) to keep the table as raw text.
        var parser = new MarkdigMarkdownParser(new KeepTablesPolicy());

        var document = parser.Parse("| a | b |\n| --- | --- |\n| x | y |");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var text = Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
        Assert.StartsWith("| a | b |", text.Text);
    }

    [Fact]
    public void CustomPolicy_IgnoringAutolinks_DropsAutolinkFromSpeech()
    {
        // Override the default (which keeps autolinks) to drop them.
        var parser = new MarkdigMarkdownParser(new IgnoreAutolinksPolicy());

        var document = parser.Parse("see <https://example.com> end");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertAllText(paragraph.Inlines, "see  end");
    }

    private sealed class KeepTablesPolicy : IUnmodeledNodeHandlingPolicy
    {
        public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => kind switch
        {
            UnmodeledNodeKind.Table => UnmodeledNodeHandling.AsRawText,
            _ => DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind),
        };
    }

    private sealed class IgnoreAutolinksPolicy : IUnmodeledNodeHandlingPolicy
    {
        public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => kind switch
        {
            UnmodeledNodeKind.Autolink => UnmodeledNodeHandling.Ignore,
            _ => DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind),
        };
    }
}
