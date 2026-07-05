using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class DefaultCommandTests
{
    [Fact]
    public void Constructor_ExposesTextAndSpan_AndIsAGameCall()
    {
        var span = SourceSpanFactory.Span();

        var command = new DefaultCommand("Alice joins Art", span);

        Assert.Equal("Alice joins Art", command.Text);
        Assert.Equal(span, command.Span);
        Assert.IsAssignableFrom<GameCall>(command);
    }
}
