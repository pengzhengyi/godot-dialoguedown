using DialogueSystem.Markdown;

namespace DialogueSystem.Tests.Markdown;

public sealed class TextInlineTests
{
    private readonly SourceSpan _anySpan = new(0, 1);

    [Fact]
    public void Constructor_ExposesTextAndSpan_AndIsInline()
    {
        var inline = new TextInline("hello", _anySpan);

        Assert.Equal("hello", inline.Text);
        Assert.Equal(_anySpan, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }

    [Fact]
    public void Constructor_NullText_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TextInline(null!, _anySpan));
    }

    [Fact]
    public void Constructor_EmptyText_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TextInline(string.Empty, _anySpan));
    }

    [Fact]
    public void Equality_SameTextAndSpan_AreEqual()
    {
        Assert.Equal(new TextInline("hi", _anySpan), new TextInline("hi", _anySpan));
    }
}
