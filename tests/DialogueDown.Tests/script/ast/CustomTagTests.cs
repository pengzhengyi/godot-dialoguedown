using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class CustomTagTests
{
    [Fact]
    public void Constructor_PlainTag_ExposesNameNoValueAndSpan_AndIsATag()
    {
        var span = SourceSpanFactory.Span();

        var tag = new CustomTag("main", Value: null, span);

        Assert.Equal("main", tag.Name);
        Assert.Null(tag.Value);
        Assert.Equal(span, tag.Span);
        Assert.IsAssignableFrom<Tag>(tag);
        Assert.IsAssignableFrom<SpeechFragment>(tag);
    }

    [Fact]
    public void Constructor_GroupTag_ExposesValue()
    {
        var tag = new CustomTag("mood", "happy", SourceSpanFactory.Span());

        Assert.Equal("mood", tag.Name);
        Assert.Equal("happy", tag.Value);
    }
}
