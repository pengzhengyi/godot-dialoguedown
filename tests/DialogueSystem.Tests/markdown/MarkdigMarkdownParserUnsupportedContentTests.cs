using DialogueSystem.Tests.Support;

namespace DialogueSystem.Tests.Markdown;

public sealed class MarkdigMarkdownParserUnsupportedContentTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_BlockNotYetSupported_Throws()
    {
        // Lists are not mapped yet, so they fail loudly for now.
        Assert.Throws<NotSupportedException>(() => Parser.Parse("- item"));
    }

    [Fact]
    public void Parse_Image_NotSupported_Throws()
    {
        // Images are not jumps; they will flatten to text later, so they throw now.
        Assert.Throws<NotSupportedException>(() => Parser.Parse("![alt](image.png)"));
    }

    [Fact]
    public void Parse_SoftLineBreak_NotSupported_Throws()
    {
        // Multiple lines in one paragraph (a soft break) are handled in a later slice.
        Assert.Throws<NotSupportedException>(() => Parser.Parse("line one\nline two"));
    }
}
