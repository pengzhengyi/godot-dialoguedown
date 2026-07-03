using DialogueSystem.Tests.Support;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserUnsupportedContentTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_BlockNotYetSupported_Throws()
    {
        // Blockquotes are not mapped yet, so they fail loudly for now.
        Assert.Throws<NotSupportedException>(() => Parser.Parse("> quote"));
    }

    [Fact]
    public void Parse_Image_NotSupported_Throws()
    {
        // Images are not jumps; they will flatten to text later, so they throw now.
        Assert.Throws<NotSupportedException>(() => Parser.Parse("![alt](image.png)"));
    }
}
