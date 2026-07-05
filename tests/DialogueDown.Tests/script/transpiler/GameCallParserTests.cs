using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using Superpower;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class GameCallParserTests
{
    [Fact]
    public void Parse_QuotedString_IsAQueryWithThatKey()
    {
        var span = SourceSpanFactory.Span(2, 5);

        var call = GameCallParser.Parse("\"Alice.FavoriteColor\"", span);

        var query = Assert.IsType<Query>(call);
        Assert.Equal("Alice.FavoriteColor", query.Key);
        Assert.Equal(span, query.Span);
    }

    [Fact]
    public void Parse_ParenthesizedText_IsADefaultCommand()
    {
        var call = GameCallParser.Parse("""("Alice joins Art")""", SourceSpanFactory.Span());

        var command = Assert.IsType<DefaultCommand>(call);
        Assert.Equal("Alice joins Art", command.Action);
    }

    [Fact]
    public void Parse_NameWithArguments_IsACustomCommand()
    {
        var call = GameCallParser.Parse("""JoinClub("Alice", "Art")""", SourceSpanFactory.Span());

        var command = Assert.IsType<CustomCommand>(call);
        Assert.Equal("JoinClub", command.Name);
        Assert.Equal(["Alice", "Art"], command.Args);
    }

    [Fact]
    public void Parse_NameWithNoArguments_IsACustomCommandWithEmptyArgs()
    {
        var call = GameCallParser.Parse("JoinClub()", SourceSpanFactory.Span());

        var command = Assert.IsType<CustomCommand>(call);
        Assert.Equal("JoinClub", command.Name);
        Assert.Empty(command.Args);
    }

    [Fact]
    public void Parse_WhitespaceAroundArgumentComma_IsTolerated()
    {
        var call = GameCallParser.Parse(
            """JoinClub("Alice" , "Photography")""", SourceSpanFactory.Span());

        var command = Assert.IsType<CustomCommand>(call);
        Assert.Equal(["Alice", "Photography"], command.Args);
    }

    [Fact]
    public void Parse_NotAGameCall_ThrowsDialogueSyntaxErrorAtTheSpan()
    {
        var span = SourceSpanFactory.Span(7, 3);

        var error = Assert.Throws<DialogueSyntaxError>(
            () => GameCallParser.Parse("just some words", span));

        Assert.Equal(span, error.Span);
        Assert.Contains("game call", error.Message);
        Assert.IsType<ParseException>(error.InnerException);
    }

    [Fact]
    public void Parse_QueryKeyWithInnerTag_KeepsTheTagLiteral()
    {
        // A tag inside a game call is part of its content, not a parsed tag.
        var call = GameCallParser.Parse("\"mood is #happy\"", SourceSpanFactory.Span());

        var query = Assert.IsType<Query>(call);
        Assert.Equal("mood is #happy", query.Key);
    }

    [Theory]
    [InlineData("JoinClub")]                        // a name with no parentheses
    [InlineData("""JoinClub["Alice"]""")]           // brackets instead of parentheses
    [InlineData("""JoinClub{"Alice"}""")]           // braces instead of parentheses
    [InlineData("""JoinClub("Alice"]""")]           // mismatched closing bracket
    [InlineData("""JoinClub("Alice",""")]           // unclosed argument list
    [InlineData("just some words")]                 // not a game call at all
    public void Parse_Malformed_ThrowsDialogueSyntaxError(string content)
    {
        Assert.Throws<DialogueSyntaxError>(
            () => GameCallParser.Parse(content, SourceSpanFactory.Span()));
    }

    [Theory]
    [InlineData("'Alice.FavoriteColor'")]      // single quotes
    [InlineData("`Alice.FavoriteColor`")]      // backticks
    [InlineData("“Alice.FavoriteColor”")]      // curly double quotes
    [InlineData("『Alice.FavoriteColor』")]     // Chinese quotes
    public void Parse_NonStraightQuotes_ThrowDialogueSyntaxError(string content)
    {
        // A game call is code-like: only straight ASCII double quotes delimit a
        // string, so smart or CJK quotes are rejected rather than guessed at.
        Assert.Throws<DialogueSyntaxError>(
            () => GameCallParser.Parse(content, SourceSpanFactory.Span()));
    }
}
