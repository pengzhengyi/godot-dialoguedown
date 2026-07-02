using DialogueSystem.Markdown;

namespace DialogueSystem.Tests.Markdown;

public sealed class CodeSpanInlineTests
{
    private readonly SourceSpan _anySpan = new(0, 1);

    [Fact]
    public void Constructor_ExposesContentAndSpan_AndIsInline()
    {
        var inline = new CodeSpanInline("\"Alice.FavoriteColor\"", _anySpan);

        Assert.Equal("\"Alice.FavoriteColor\"", inline.Content);
        Assert.Equal(_anySpan, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }
}
