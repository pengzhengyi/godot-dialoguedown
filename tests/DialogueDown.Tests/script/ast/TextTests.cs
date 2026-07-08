using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class TextTests
{
    [Fact]
    public void Constructor_ExposesContentAndSpan_AndIsAInlineFragment()
    {
        var span = SourceSpanFactory.Span();

        var text = new Text("Hello there", span);

        Assert.Equal("Hello there", text.Content);
        Assert.Equal(span, text.Span);
        Assert.IsAssignableFrom<InlineFragment>(text);
        Assert.IsAssignableFrom<ScriptNode>(text);
    }

    [Fact]
    public void Constructor_NullContent_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new Text(null!, SourceSpanFactory.Span()));

    [Fact]
    public void Constructor_EmptyContent_Throws() =>
        Assert.Throws<ArgumentException>(() => new Text(string.Empty, SourceSpanFactory.Span()));
}
