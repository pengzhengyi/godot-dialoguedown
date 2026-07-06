using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class ParserTests
{
    [Fact]
    public void ParseAll_WholeInput_ReturnsTheValue() =>
        Assert.Equal("abc", new IdentifierParser().ParseAll(ParseInputFactory.Input("abc")));

    [Fact]
    public void ParseAll_NonZeroPosition_StillConsumesTheWholeText() =>
        // Position only anchors spans; it does not shift where parsing starts, so a
        // fully-consumed text succeeds regardless of the anchor.
        Assert.Equal("abc", new IdentifierParser().ParseAll(ParseInputFactory.Input("abc", 5)));

    [Fact]
    public void ParseAll_Failure_AnchorsTheErrorSpanAtPosition()
    {
        var error = Assert.Throws<DialogueSyntaxError>(
            () => new IdentifierParser().ParseAll(ParseInputFactory.Input("123", 5)));

        Assert.Equal(5, error.Span.Start);
    }

    [Fact]
    public void ParseAll_Rejected_AppendsTheTechnicalReason()
    {
        var error = Assert.Throws<DialogueSyntaxError>(
            () => new IdentifierParser().ParseAll(ParseInputFactory.Input("123")));

        Assert.StartsWith("\"123\" is not valid here.\n  ↳ ", error.Message);
    }

    [Fact]
    public void ParseAll_IncompleteMatch_ReportsUnconsumedText()
    {
        // The grammar matches "abc" but leaves " def" behind — a different failure
        // than an outright rejection, with its own message and no technical reason.
        var error = Assert.Throws<DialogueSyntaxError>(
            () => new IdentifierParser().ParseAll(ParseInputFactory.Input("abc def")));

        Assert.Equal("Cannot match the full text \"abc def\".", error.Message);
    }

    [Fact]
    public void ParseAll_Rejected_UsesSubclassExplanation()
    {
        var error = Assert.Throws<DialogueSyntaxError>(
            () => new LabelledParser().ParseAll(ParseInputFactory.Input("x")));

        Assert.Equal("x is not a valid label.\n  ↳ nope", error.Message);
    }

    [Fact]
    public void ParseAll_Rejected_UsesSubclassMessageOverride()
    {
        var error = Assert.Throws<DialogueSyntaxError>(
            () => new CustomFormatParser().ParseAll(ParseInputFactory.Input("x")));

        Assert.Equal("x -- nope", error.Message);
    }

    [Fact]
    public void ParseAll_IncompleteMatch_UsesSubclassOverride()
    {
        var error = Assert.Throws<DialogueSyntaxError>(
            () => new CustomIncompleteParser().ParseAll(ParseInputFactory.Input("abc def")));

        Assert.Equal("leftover in abc def", error.Message);
    }

    private class IdentifierParser : Parser<string>
    {
        public override ParseResult<string> Consume(ParseInput input) =>
            TestParsers.Identifier.Consume(input);

        protected override string DescribeFailure(string text) => $"\"{text}\" is not valid here.";
    }

    private class LabelledParser : Parser<string>
    {
        public override ParseResult<string> Consume(ParseInput input) =>
            ParseResult<string>.Fail(new ParseError("nope"));

        protected override string DescribeFailure(string text) => $"{text} is not a valid label.";
    }

    private sealed class CustomFormatParser : LabelledParser
    {
        protected override string DescribeFailure(string text) => text;

        protected override string BuildErrorMessage(string text, string reason) =>
            $"{text} -- {reason}";
    }

    private sealed class CustomIncompleteParser : Parser<string>
    {
        // Matches only the leading "abc", leaving the rest unconsumed.
        public override ParseResult<string> Consume(ParseInput input) =>
            ParseResult<string>.Ok(new ParseMatch<string>("abc", new TextRange(input.Position, 3)));

        protected override string DescribeFailure(string text) => text;

        protected override string DescribeIncompleteMatch(string text) => $"leftover in {text}";
    }
}
