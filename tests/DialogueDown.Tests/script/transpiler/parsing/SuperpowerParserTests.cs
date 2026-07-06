using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class SuperpowerParserTests
{
    private static readonly IParser<string> _identifier =
        SuperpowerParser.Wrap(Identifier.CStyle.Select(name => name.ToStringValue()));

    [Fact]
    public void Consume_ConsumesAPrefix_AndReportsTheAbsoluteRange()
    {
        var result = _identifier.Consume(ParseInputFactory.Input("abc def", 5));

        Assert.True(result.Success);
        Assert.Equal("abc", result.Match.Value);
        Assert.Equal(5, result.Match.Range.Start);
        Assert.Equal(3, result.Match.Range.Length);
    }

    [Fact]
    public void Consume_NoMatch_FailsWithAnError()
    {
        var result = _identifier.Consume(ParseInputFactory.Input("123"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
