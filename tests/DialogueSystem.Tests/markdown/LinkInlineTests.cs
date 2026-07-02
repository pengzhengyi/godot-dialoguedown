using DialogueSystem.Markdown;

namespace DialogueSystem.Tests.Markdown;

public sealed class LinkInlineTests
{
    private readonly SourceSpan _anySpan = new(0, 1);

    [Fact]
    public void Constructor_ExposesTargetLabelAndSpan_AndIsInline()
    {
        var inline = new LinkInline("#play-tennis", "Play tennis", _anySpan);

        Assert.Equal("#play-tennis", inline.Target);
        Assert.Equal("Play tennis", inline.Label);
        Assert.Equal(_anySpan, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }
}
