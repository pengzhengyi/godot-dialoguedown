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
    public void Parse_InlineNotYetSupported_Throws()
    {
        // A code span is not mapped yet, so it fails loudly for now.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("`code`"));
    }
}
