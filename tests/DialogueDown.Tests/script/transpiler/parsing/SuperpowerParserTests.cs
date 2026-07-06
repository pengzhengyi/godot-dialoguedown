using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class SuperpowerParserTests
{
    [Fact]
    public void Consume_ConsumesAPrefix_AndReportsTheAbsoluteRange()
    {
        var result = TestParsers.Identifier.Consume(ParseInputFactory.Input("abc def", 5));

        Assert.True(result.Success);
        Assert.Equal("abc", result.MatchedValue);
        Assert.Equal(5, result.MatchedRange.Start);
        Assert.Equal(3, result.MatchedRange.Length);
    }

    [Fact]
    public void Consume_NoMatch_FailsWithAnError()
    {
        var result = TestParsers.Identifier.Consume(ParseInputFactory.Input("123"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
