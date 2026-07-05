using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class TagParserTests
{
    [Fact]
    public void Parse_CustomPlainTag_IsCustomTagWithNameAndSpan()
    {
        var span = SourceSpanFactory.Span(1, 4);

        var tag = AssertCustomTag(TagParser.Parse("#main", span), "main");

        Assert.Equal(span, tag.Span);
    }

    [Fact]
    public void Parse_ReservedPlainTag_IsReservedTag() =>
        AssertReservedTag(TagParser.Parse("##default", SourceSpanFactory.Span()), "default");

    [Fact]
    public void Parse_CustomGroup_HasNameAndValue() =>
        AssertCustomTag(TagParser.Parse("#mood=happy", SourceSpanFactory.Span()), "mood", "happy");

    [Fact]
    public void Parse_ReservedGroup_HasNameAndValue() =>
        AssertReservedTag(
            TagParser.Parse("##mode=silent", SourceSpanFactory.Span()), "mode", "silent");

    [Fact]
    public void Parse_QuotedNames_AllowSpaces() =>
        AssertCustomTag(
            TagParser.Parse(
                """
                #"speaker tone"="warm"
                """,
                SourceSpanFactory.Span()),
            "speaker tone",
            "warm");

    [Theory]
    [InlineData("main")]        // no # prefix
    [InlineData("#")]           // a prefix with no name
    [InlineData("#=warm")]      // a value with no name
    [InlineData("#mood=")]      // a group with no value
    [InlineData("###main")]     // three hashes is not a valid prefix
    public void Parse_Malformed_ThrowsDialogueSyntaxError(string content)
    {
        Assert.Throws<DialogueSyntaxError>(
            () => TagParser.Parse(content, SourceSpanFactory.Span()));
    }
}
