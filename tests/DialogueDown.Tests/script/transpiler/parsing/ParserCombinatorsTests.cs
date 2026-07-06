using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class ParserCombinatorsTests
{
    [Fact]
    public void Select_TransformsTheValue_KeepingTheConsumedRange()
    {
        var parser = TestParsers.Identifier.Select(name => name.ToUpperInvariant());

        var result = parser.Consume(ParseInputFactory.Input("abc def", 5));

        Assert.Equal("ABC", result.MatchedValue);
        Assert.Equal(5, result.MatchedRange.Start);
        Assert.Equal(3, result.MatchedRange.Length);
    }

    [Fact]
    public void Select_WithSpan_ExposesTheConsumedSourceSpan()
    {
        var parser = TestParsers.Identifier.Select((name, span) => $"{name}@{span.Start}:{span.Length}");

        var result = parser.Consume(ParseInputFactory.Input("abc", 5));

        Assert.Equal("abc@5:3", result.MatchedValue);
    }

    [Fact]
    public void Select_PropagatesFailure()
    {
        var parser = TestParsers.Identifier.Select(name => name.ToUpperInvariant());

        var result = parser.Consume(ParseInputFactory.Input("123"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
