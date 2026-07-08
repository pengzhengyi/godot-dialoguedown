using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class JumpIndicatorTests
{
    [Fact]
    public void Constructor_ExposesSpan_AndIsAInlineFragment()
    {
        var span = SourceSpanFactory.Span();

        var indicator = new JumpIndicator(span);

        Assert.Equal(span, indicator.Span);
        Assert.IsAssignableFrom<InlineFragment>(indicator);
        Assert.IsAssignableFrom<ScriptNode>(indicator);
    }
}
