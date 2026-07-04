using DialogueSystem.Markdown;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigUnmodeledNodeClassifierTests
{
    [Fact]
    public void ClassifyBlock_MapsKnownBlocks()
    {
        Assert.Equal(UnmodeledNodeKind.CodeBlock, MarkdigUnmodeledNodeClassifier.ClassifyBlock(new FencedCodeBlock(null!)));
        Assert.Equal(UnmodeledNodeKind.ThematicBreak, MarkdigUnmodeledNodeClassifier.ClassifyBlock(new ThematicBreakBlock(null!)));
        Assert.Equal(UnmodeledNodeKind.Table, MarkdigUnmodeledNodeClassifier.ClassifyBlock(new Table()));
        Assert.Equal(UnmodeledNodeKind.BlockQuote, MarkdigUnmodeledNodeClassifier.ClassifyBlock(new QuoteBlock(null!)));
        Assert.Equal(UnmodeledNodeKind.RawHtml, MarkdigUnmodeledNodeClassifier.ClassifyBlock(new HtmlBlock(null!)));
    }

    [Fact]
    public void ClassifyBlock_UnrecognizedBlock_IsOther()
    {
        // A block the classifier does not call out falls back to Other.
        Assert.Equal(UnmodeledNodeKind.Other, MarkdigUnmodeledNodeClassifier.ClassifyBlock(new ParagraphBlock()));
    }

    [Fact]
    public void ClassifyInline_MapsKnownInlines()
    {
        Assert.Equal(UnmodeledNodeKind.Autolink, MarkdigUnmodeledNodeClassifier.ClassifyInline(new AutolinkInline("https://x")));
        Assert.Equal(UnmodeledNodeKind.RawHtml, MarkdigUnmodeledNodeClassifier.ClassifyInline(new HtmlInline("<b>")));
    }

    [Fact]
    public void ClassifyInline_UnrecognizedInline_IsOther()
    {
        // An inline the classifier does not call out falls back to Other.
        Assert.Equal(UnmodeledNodeKind.Other, MarkdigUnmodeledNodeClassifier.ClassifyInline(new LiteralInline("x")));
    }
}
