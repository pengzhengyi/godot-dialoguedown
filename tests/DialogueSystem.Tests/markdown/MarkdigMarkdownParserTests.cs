using DialogueSystem.Markdown;
using static DialogueSystem.Tests.Support.MarkdownAstAssert;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserTests
{
    private readonly IMarkdownParser _parser = new MarkdigMarkdownParser();

    [Fact]
    public void Parse_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void Parse_EmptyOrWhitespace_ReturnsEmptyDocument(string source)
    {
        var document = _parser.Parse(source);

        Assert.Empty(document.Blocks);
    }

    [Fact]
    public void Parse_PlainParagraph_ProducesParagraphWithRawText()
    {
        var document = _parser.Parse("Hello, Bob!");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Hello, Bob!");
    }

    [Theory]
    [InlineData("I *really* mean it")]
    [InlineData("keep_the_underscores")]
    public void Parse_EmphasisMarkers_StayLiteral(string source)
    {
        var document = _parser.Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, source);
    }

    [Theory]
    [InlineData("# One", 1, "One")]
    [InlineData("###### Six", 6, "Six")]
    public void Parse_Heading_ProducesHeadingWithLevelAndText(string source, int level, string expected)
    {
        var document = _parser.Parse(source);

        var heading = AssertSingleBlock<Heading>(document);
        Assert.Equal(level, heading.Level);
        AssertSingleText(heading.Inlines, expected);
    }

    [Theory]
    [InlineData("##     Title")]
    [InlineData("##   Title   ")]
    public void Parse_HeadingWithSurroundingSpaces_KeepsOnlyContent(string source)
    {
        // CommonMark trims the spaces around heading content, so the text is clean.
        var document = _parser.Parse(source);

        var heading = AssertSingleBlock<Heading>(document);
        AssertSingleText(heading.Inlines, "Title");
    }

    [Fact]
    public void Parse_EmptyHeading_HasNoInlinesAndSpansTheHashes()
    {
        var document = _parser.Parse("##");

        var heading = AssertSingleBlock<Heading>(document);
        Assert.Equal(2, heading.Level);
        Assert.Empty(heading.Inlines);
        Assert.Equal(new SourceSpan(0, 2), heading.Span);
    }

    [Fact]
    public void Parse_HashNotAtLineStart_IsLiteralText()
    {
        // Only a leading '#' starts a heading; a '#' inside a line stays literal.
        var document = _parser.Parse("Alice #tag");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Alice #tag");
    }

    [Fact]
    public void Parse_BlockNotYetSupported_Throws()
    {
        // Lists are not mapped yet, so they fail loudly for now.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("- item"));
    }

    [Fact]
    public void Parse_Link_ProducesLinkInlineWithTargetAndLabel()
    {
        var document = _parser.Parse("[Play tennis](#play-tennis)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#play-tennis", link.Target);
        Assert.Equal("Play tennis", link.Label);
    }

    [Fact]
    public void Parse_LinkLabelWithEmphasisMarkers_FlattensToRawText()
    {
        // Emphasis is disabled, so markers in a label stay literal in the flattened text.
        var document = _parser.Parse("[Play *tennis*](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Equal("Play *tennis*", link.Label);
    }

    [Fact]
    public void Parse_LinkWithEmptyTarget_IsAccepted()
    {
        // Syntactically valid; an empty jump target is rejected later, not here.
        var document = _parser.Parse("[label]()");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal(string.Empty, link.Target);
        Assert.Equal("label", link.Label);
    }

    [Fact]
    public void Parse_CodeSpan_ProducesCodeSpanInlineWithRawContent()
    {
        var document = _parser.Parse("`\"Alice.FavoriteColor\"`");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var code = AssertSingleInline<CodeSpanInline>(paragraph.Inlines);
        Assert.Equal("\"Alice.FavoriteColor\"", code.Content);
    }

    [Fact]
    public void Parse_CodeSpanWithMarkers_KeepsRawContent()
    {
        // Anything inside a code span is verbatim, including would-be styling markers.
        var document = _parser.Parse("`it *stays* raw`");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var code = AssertSingleInline<CodeSpanInline>(paragraph.Inlines);
        Assert.Equal("it *stays* raw", code.Content);
    }

    [Fact]
    public void Parse_WhitespaceOnlyCodeSpan_IsAccepted()
    {
        // Syntactically valid; an empty/whitespace command is rejected later, not here.
        var document = _parser.Parse("` `");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var code = AssertSingleInline<CodeSpanInline>(paragraph.Inlines);
        Assert.Equal(" ", code.Content);
    }

    [Fact]
    public void Parse_LinkWithFormattedLabel_Throws()
    {
        // Jump labels are expected to be plain text; a formatted label (here a
        // code span inside the label) is not supported.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("[a `b` c](#x)"));
    }

    [Fact]
    public void Parse_Image_NotSupported_Throws()
    {
        // Images are not jumps; they will flatten to text later, so they throw now.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("![alt](image.png)"));
    }

    [Fact]
    public void Parse_SoftLineBreak_NotSupported_Throws()
    {
        // Multiple lines in one paragraph (a soft break) are handled in a later slice.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("line one\nline two"));
    }
}
