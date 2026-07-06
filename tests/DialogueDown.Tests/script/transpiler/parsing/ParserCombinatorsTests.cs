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

    [Fact]
    public void SelectMany_SequencesParsers_ThreadingTheAbsolutePosition()
    {
        var parser =
            from first in TestParsers.Identifier
            from _ in TestParsers.Symbol('-')
            from second in TestParsers.Identifier
            select $"{first}+{second}";

        var result = parser.Consume(ParseInputFactory.Input("ab-cd", 10));

        Assert.Equal("ab+cd", result.MatchedValue);
        Assert.Equal(10, result.MatchedRange.Start);
        Assert.Equal(5, result.MatchedRange.Length);
    }

    [Fact]
    public void SelectMany_PropagatesFailureOfTheFirstParser()
    {
        var parser =
            from first in TestParsers.Identifier
            from second in TestParsers.Identifier
            select $"{first}{second}";

        var result = parser.Consume(ParseInputFactory.Input("-nope"));

        Assert.False(result.Success);
    }

    [Fact]
    public void SelectMany_PropagatesFailureOfTheSecondParser()
    {
        var parser =
            from first in TestParsers.Identifier
            from _ in TestParsers.Symbol('-')
            from second in TestParsers.Identifier
            select $"{first}+{second}";

        // "ab-" has no second identifier, so the second parser fails.
        var result = parser.Consume(ParseInputFactory.Input("ab-"));

        Assert.False(result.Success);
    }
}
