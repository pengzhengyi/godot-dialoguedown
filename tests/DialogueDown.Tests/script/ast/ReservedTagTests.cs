using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ReservedTagTests
{
    [Fact]
    public void Constructor_ExposesNameValueAndSpan_AndIsATag()
    {
        var span = SourceSpanFactory.Span();

        var tag = new ReservedTag("default", Value: null, span);

        Assert.Equal("default", tag.Name);
        Assert.Null(tag.Value);
        Assert.Equal(span, tag.Span);
        Assert.IsAssignableFrom<Tag>(tag);
    }

    [Fact]
    public void Constructor_GroupTag_ExposesValue()
    {
        var tag = new ReservedTag("mode", "silent", SourceSpanFactory.Span());

        Assert.Equal("silent", tag.Value);
    }
}
