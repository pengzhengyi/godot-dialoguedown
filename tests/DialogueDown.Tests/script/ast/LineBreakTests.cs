using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class LineBreakTests
{
    [Fact]
    public void Constructor_ExposesSpan_AndIsAInlineFragment()
    {
        var span = SourceSpanFactory.Span();

        var lineBreak = new LineBreak(span);

        Assert.Equal(span, lineBreak.Span);
        Assert.IsAssignableFrom<InlineFragment>(lineBreak);
        Assert.IsAssignableFrom<ScriptNode>(lineBreak);
    }
}
