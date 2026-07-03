using DialogueSystem.Markdown;

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

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
        var text = Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
        Assert.Equal("Hello, Bob!", text.Text);
    }

    [Fact]
    public void Parse_ContentNotYetSupported_Throws()
    {
        // Block mapping is added incrementally; until a block type is supported,
        // parsing content that uses it fails loudly rather than silently.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("# Heading"));
    }

    [Theory]
    [InlineData("I *really* mean it")]
    [InlineData("keep_the_underscores")]
    public void Parse_EmphasisMarkers_StayLiteral(string source)
    {
        var document = _parser.Parse(source);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
        var text = Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
        Assert.Equal(source, text.Text);
    }

    [Fact]
    public void Parse_InlineNotYetSupported_Throws()
    {
        // A code span is not mapped yet, so it fails loudly for now.
        Assert.Throws<NotSupportedException>(() => _parser.Parse("`code`"));
    }
}
