using DialogueSystem.Markdown;
using DialogueSystem.Tests.Support;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserEmphasisTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("*word*")]
    [InlineData("_word_")]
    public void Parse_SingleDelimiter_ProducesItalic(string source)
    {
        var emphasis = SingleEmphasis(source);

        Assert.Equal(EmphasisKind.Italic, emphasis.Kind);
        AssertSingleText(emphasis.Children, "word");
    }

    [Theory]
    [InlineData("**word**")]
    [InlineData("__word__")]
    public void Parse_DoubleDelimiter_ProducesBold(string source)
    {
        var emphasis = SingleEmphasis(source);

        Assert.Equal(EmphasisKind.Bold, emphasis.Kind);
        AssertSingleText(emphasis.Children, "word");
    }

    [Fact]
    public void Parse_BoldItalic_NestsEmphasis()
    {
        // ***x*** is italic wrapping bold in Markdig, so nesting covers bold-italic.
        var document = Parser.Parse("***word***");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var outer = AssertSingleInline<EmphasisInline>(paragraph.Inlines);
        Assert.Equal(EmphasisKind.Italic, outer.Kind);
        var inner = AssertSingleInline<EmphasisInline>(outer.Children);
        Assert.Equal(EmphasisKind.Bold, inner.Kind);
        AssertSingleText(inner.Children, "word");
    }

    [Fact]
    public void Parse_CodeSpanInsideBold_StaysParsed()
    {
        // A query (code span) inside bold keeps its structure, not frozen as text.
        var document = Parser.Parse("**Hello `\"X.Name\"`!**");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var bold = AssertSingleInline<EmphasisInline>(paragraph.Inlines);
        Assert.Equal(EmphasisKind.Bold, bold.Kind);
        Assert.Collection(
            bold.Children,
            inline => AssertText(inline, "Hello "),
            inline => Assert.Equal("\"X.Name\"", Assert.IsType<CodeSpanInline>(inline).Content),
            inline => AssertText(inline, "!"));
    }

    [Fact]
    public void Parse_LinkInsideEmphasis_StaysParsed()
    {
        // A jump (link) inside emphasis keeps its target and label.
        var document = Parser.Parse("*go [here](#x)*");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var emphasis = AssertSingleInline<EmphasisInline>(paragraph.Inlines);
        Assert.Collection(
            emphasis.Children,
            inline => AssertText(inline, "go "),
            inline =>
            {
                var link = Assert.IsType<LinkInline>(inline);
                Assert.Equal("#x", link.Target);
                Assert.Equal("here", link.Label);
            });
    }

    [Fact]
    public void Parse_EmphasisAmongText_KeepsSurroundingText()
    {
        var document = Parser.Parse("I *really* mean it");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "I "),
            inline =>
            {
                var emphasis = Assert.IsType<EmphasisInline>(inline);
                Assert.Equal(EmphasisKind.Italic, emphasis.Kind);
                AssertSingleText(emphasis.Children, "really");
            },
            inline => AssertText(inline, " mean it"));
    }

    [Theory]
    [InlineData(@"\*not styled\*", "*not styled*")]
    [InlineData("keep_the_underscores", "keep_the_underscores")]
    public void Parse_NonEmphasis_StaysLiteralText(string source, string expected)
    {
        // Escaped asterisks and intraword underscores never form emphasis.
        var document = Parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertAllText(paragraph.Inlines, expected);
    }

    private EmphasisInline SingleEmphasis(string source)
    {
        var paragraph = AssertSingleBlock<Paragraph>(Parser.Parse(source));
        return AssertSingleInline<EmphasisInline>(paragraph.Inlines);
    }
}
