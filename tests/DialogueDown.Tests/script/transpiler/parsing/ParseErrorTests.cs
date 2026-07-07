using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class ParseErrorTests
{
    [Fact]
    public void Detail_CarriesTheReason() =>
        Assert.Equal("unexpected '#'", new ParseError("unexpected '#'").Detail);
}
