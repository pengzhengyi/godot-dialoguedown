using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class SpeakerBuilderTests
{
    private static readonly SpeakerBuilder _builder = new(SpeakerPrefixParser.Prefix, new TagBuilder());

    [Fact]
    public void NameWithIdAndTags_IsADeclaration() =>
        AssertSpeakerDeclaration(Speaker("Alice @A #main: Hello"), "Alice", "A", CustomTag("main"));

    [Fact]
    public void NameWithTagOnly_IsADeclaration() =>
        AssertSpeakerDeclaration(Speaker("Alice #main: Hi"), "Alice", tags: CustomTag("main"));

    [Fact]
    public void NameWithReservedTag_IsADeclaration() =>
        AssertSpeakerDeclaration(Speaker("Alice ##vip: Hi"), "Alice", tags: ReservedTag("vip"));

    [Fact]
    public void BareName_IsANameReference() =>
        AssertSpeakerNameReference(Speaker("Alice: Hello"), "Alice");

    [Fact]
    public void BareId_IsAnIdReference() =>
        AssertSpeakerIdReference(Speaker("@A: Hello"), "A");

    [Fact]
    public void IdWithTags_IsAPartialDeclaration() =>
        // An id plus tags (no name) references a speaker and contributes tags,
        // resolved to a full declaration later.
        AssertPartialSpeakerDeclaration(Speaker("@alice #excited #loud: Hi"), "alice",
            CustomTag("excited"), CustomTag("loud"));

    [Fact]
    public void Declaration_TagCarriesItsOwnAbsoluteSpan()
    {
        var text = "Alice #mood=happy: Hi";

        var result = _builder.Build(ParseInputFactory.Input(text, 10));

        Assert.True(result.Success);
        var declaration = Assert.IsType<SpeakerDeclaration>(result.MatchedValue);
        var tag = Assert.Single(declaration.Tags);
        Assert.Equal(10 + text.IndexOf('#'), tag.Span.Start);
    }

    [Fact]
    public void SpeechStart_IsTheConsumedLength_JustAfterTheColon()
    {
        var text = "Alice: Hello";

        var result = _builder.Build(ParseInputFactory.Input(text));

        Assert.True(result.Success);
        Assert.Equal(text.IndexOf(':') + 1, result.MatchedLength);
    }

    [Fact]
    public void ColonInsideSpeech_IsNotASpeaker() =>
        AssertBuildFailed("The time is 3:00");

    [Fact]
    public void UnquotedMultiWordName_IsNotASpeaker() =>
        AssertBuildFailed("Old Man: Hello");

    [Fact]
    public void BareColon_IsNotASpeaker() =>
        AssertBuildFailed(": Hello");

    [Fact]
    public void TagsWithoutNameOrId_ThrowsDialogueSyntaxError()
    {
        var error = Assert.Throws<DialogueSyntaxError>(() => Build("#lonely: Hi"));

        Assert.Contains("names no speaker", error.Message);
    }

    private static ParseResult<Speaker> Build(string text) =>
        _builder.Build(ParseInputFactory.Input(text));

    private static Speaker Speaker(string text)
    {
        var result = Build(text);
        Assert.True(result.Success);
        return result.MatchedValue;
    }

    private static void AssertBuildFailed(string text) => Assert.False(Build(text).Success);
}
