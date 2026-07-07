using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class TagParserTests
{
    [Fact]
    public void CustomPlainTag_HasNameAndNoValue()
    {
        var tag = Parse("#main");

        Assert.False(tag.IsReserved);
        Assert.Equal("main", tag.Name);
        Assert.Null(tag.Value);
    }

    [Fact]
    public void ReservedPlainTag_IsReserved()
    {
        var tag = Parse("##default");

        Assert.True(tag.IsReserved);
        Assert.Equal("default", tag.Name);
    }

    [Fact]
    public void CustomGroup_HasNameAndValue()
    {
        var tag = Parse("#mood=happy");

        Assert.Equal("mood", tag.Name);
        Assert.Equal("happy", tag.Value);
    }

    [Fact]
    public void ReservedGroup_HasNameAndValue()
    {
        var tag = Parse("##mode=silent");

        Assert.True(tag.IsReserved);
        Assert.Equal("mode", tag.Name);
        Assert.Equal("silent", tag.Value);
    }

    [Fact]
    public void QuotedNames_AllowSpaces()
    {
        var tag = Parse(
            """
            #"speaker tone"="warm"
            """);

        Assert.Equal("speaker tone", tag.Name);
        Assert.Equal("warm", tag.Value);
    }

    [Theory]
    [InlineData("main")]        // no # prefix
    [InlineData("#")]           // a prefix with no name
    [InlineData("#=warm")]      // a value with no name
    [InlineData("#mood=")]      // a group with no value
    [InlineData("###main")]     // three hashes is not a valid prefix
    public void Malformed_DoesNotMatchACompleteTag(string content)
    {
        var result = TagParser.Token.Consume(ParseInputFactory.Input(content));

        // Either it fails outright, or it matches only a prefix of the text — never
        // the whole thing as one valid tag.
        Assert.False(result.Success && result.MatchedLength == content.Length);
    }

    private static TagData Parse(string content)
    {
        var result = TagParser.Token.Consume(ParseInputFactory.Input(content));
        Assert.True(result.Success);
        return result.MatchedValue;
    }
}
