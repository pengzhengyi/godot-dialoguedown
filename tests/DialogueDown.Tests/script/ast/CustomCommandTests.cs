using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class CustomCommandTests
{
    [Fact]
    public void Constructor_ExposesNameArgsAndSpan_AndIsAGameCall()
    {
        var span = SourceSpanFactory.Span();

        var command = new CustomCommand("JoinClub", ["Alice", "Art"], span);

        Assert.Equal("JoinClub", command.Name);
        Assert.Equal(["Alice", "Art"], command.Args);
        Assert.Equal(span, command.Span);
        Assert.IsAssignableFrom<GameCall>(command);
    }
}
