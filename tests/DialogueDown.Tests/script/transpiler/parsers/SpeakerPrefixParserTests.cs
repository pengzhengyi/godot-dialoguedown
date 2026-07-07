using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class SpeakerPrefixParserTests
{
    [Fact]
    public void NameIdAndTags_AreAllReported()
    {
        var data = Data("Alice @A #main: Hello");

        Assert.Equal("Alice", data.Name);
        Assert.Equal("A", data.Id);
        var tag = Assert.Single(data.Tags);
        Assert.Equal("main", tag.Value.Name);
        Assert.False(tag.Value.IsReserved);
    }

    [Fact]
    public void BareName_HasNoIdOrTags()
    {
        var data = Data("Alice: Hello");

        Assert.Equal("Alice", data.Name);
        Assert.Null(data.Id);
        Assert.Empty(data.Tags);
    }

    [Fact]
    public void QuotedName_AllowsSpaces() =>
        Assert.Equal("Old Man", Data("\"Old Man\": Hello").Name);

    [Fact]
    public void BareId_HasNoName() =>
        Assert.Equal("A", Data("@A: Hello").Id);

    [Fact]
    public void ReservedTag_IsReported()
    {
        var tag = Assert.Single(Data("Alice ##vip: Hi").Tags);

        Assert.True(tag.Value.IsReserved);
        Assert.Equal("vip", tag.Value.Name);
    }

    [Fact]
    public void GluedIdAndTag_IsNotAPrefix() =>
        // No whitespace between the id and the tag, so this is not a speaker prefix.
        AssertNotAPrefix("Alice @A#mood=happy: Hi");

    [Fact]
    public void GluedTags_AreNotAPrefix() =>
        // "#tag1#tag2" cannot be read as one tag or two, so it is not a prefix.
        AssertNotAPrefix("Alice @A #tag1#tag2: Hi");

    [Fact]
    public void WhitespaceSeparatedTags_AreAllReported()
    {
        var data = Data("Alice #a #b: Hi");

        Assert.Equal(2, data.Tags.Count);
        Assert.Equal("a", data.Tags[0].Value.Name);
        Assert.Equal("b", data.Tags[1].Value.Name);
    }

    [Fact]
    public void NamelessTags_ParseInOrder()
    {
        var data = Data("#a #b: Hi");

        Assert.Null(data.Name);
        Assert.Null(data.Id);
        Assert.Equal("a", data.Tags[0].Value.Name);
        Assert.Equal("b", data.Tags[1].Value.Name);
    }

    [Fact]
    public void MetadataWithoutName_StillParses_AsData()
    {
        // The parser only recognizes shape; the builder rejects nameless metadata.
        var data = Data("@A #main: Hi");

        Assert.Null(data.Name);
        Assert.Equal("A", data.Id);
        Assert.Single(data.Tags);
    }

    [Fact]
    public void BareColon_MatchesAnEmptyPrefix()
    {
        var data = Data(": Hello");

        Assert.Null(data.Name);
        Assert.Null(data.Id);
        Assert.Empty(data.Tags);
    }

    [Fact]
    public void Tag_CarriesItsOwnAbsoluteSpan()
    {
        var text = "Alice #mood=happy: Hi";

        var result = Parse(text, 10);

        Assert.True(result.Success);
        var tag = Assert.Single(result.MatchedValue.Tags);
        Assert.Equal(10 + text.IndexOf('#'), tag.Range.Start);
    }

    [Fact]
    public void ConsumedLength_IsJustAfterTheColon()
    {
        var text = "Alice: Hello";

        var result = Parse(text);

        Assert.True(result.Success);
        Assert.Equal(text.IndexOf(':') + 1, result.MatchedLength);
    }

    [Fact]
    public void ColonInsideSpeech_IsNotAPrefix() =>
        AssertNotAPrefix("The time is 3:00");

    [Fact]
    public void UnquotedMultiWordName_IsNotAPrefix() =>
        AssertNotAPrefix("Old Man: Hello");

    private static ParseResult<SpeakerPrefixData> Parse(string text, int position = 0) =>
        SpeakerPrefixParser.Prefix.Consume(ParseInputFactory.Input(text, position));

    private static SpeakerPrefixData Data(string text)
    {
        var result = Parse(text);
        Assert.True(result.Success);
        return result.MatchedValue;
    }

    private static void AssertNotAPrefix(string text) => Assert.False(Parse(text).Success);
}
