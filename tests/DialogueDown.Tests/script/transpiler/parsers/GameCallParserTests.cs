using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class GameCallParserTests
{
    [Fact]
    public void QuotedString_IsAQueryWithThatKey() =>
        Assert.Equal("Alice.FavoriteColor",
            Assert.IsType<QueryData>(Parse("\"Alice.FavoriteColor\"")).Key);

    [Fact]
    public void ParenthesizedText_IsADefaultCommand() =>
        Assert.Equal("Alice joins Art",
            Assert.IsType<DefaultCommandData>(Parse("""("Alice joins Art")""")).Action);

    [Fact]
    public void NameWithArguments_IsACustomCommand()
    {
        var command = Assert.IsType<CustomCommandData>(Parse("""JoinClub("Alice", "Art")"""));

        Assert.Equal("JoinClub", command.Name);
        Assert.Equal(["Alice", "Art"], command.Args);
    }

    [Fact]
    public void NameWithNoArguments_IsACustomCommandWithEmptyArgs()
    {
        var command = Assert.IsType<CustomCommandData>(Parse("JoinClub()"));

        Assert.Equal("JoinClub", command.Name);
        Assert.Empty(command.Args);
    }

    [Fact]
    public void WhitespaceAroundArgumentComma_IsTolerated() =>
        Assert.Equal(["Alice", "Photography"],
            Assert.IsType<CustomCommandData>(Parse("""JoinClub("Alice" , "Photography")""")).Args);

    [Fact]
    public void QueryKeyWithInnerTag_KeepsTheTagLiteral() =>
        // A tag inside a game call is part of its content, not a parsed tag.
        Assert.Equal("mood is #happy", Assert.IsType<QueryData>(Parse("\"mood is #happy\"")).Key);

    [Theory]
    [InlineData("JoinClub")]                        // a name with no parentheses
    [InlineData("""JoinClub["Alice"]""")]           // brackets instead of parentheses
    [InlineData("""JoinClub{"Alice"}""")]           // braces instead of parentheses
    [InlineData("""JoinClub("Alice"]""")]           // mismatched closing bracket
    [InlineData("""JoinClub("Alice",""")]           // unclosed argument list
    [InlineData("just some words")]                 // not a game call at all
    [InlineData("'Alice.FavoriteColor'")]           // single quotes, not straight double
    [InlineData("`Alice.FavoriteColor`")]           // backticks
    [InlineData("“Alice.FavoriteColor”")]           // curly double quotes
    [InlineData("『Alice.FavoriteColor』")]          // CJK quotes
    public void Malformed_DoesNotFullyParse(string content)
    {
        // A game call is code-like: only straight ASCII double quotes delimit a
        // string, and the whole text must be one call.
        var result = GameCallParser.Grammar.Consume(ParseInputFactory.Input(content));

        Assert.False(result.Success && result.MatchedLength == content.Length);
    }

    private static GameCallData Parse(string content)
    {
        var result = GameCallParser.Grammar.Consume(ParseInputFactory.Input(content));
        Assert.True(result.Success);
        return result.MatchedValue;
    }
}
