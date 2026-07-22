using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class SpeakerBuilderTests
{
    private static readonly SpeakerBuilder _builder =
        TranspilerBuilderFactory.SpeakerBuilder();

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

        var declaration = Assert.IsType<SpeakerDeclaration>(SpeakerAt(text, 10));

        var tag = Assert.Single(declaration.Tags);
        Assert.Equal(10 + text.IndexOf('#'), tag.Span.Start);
    }

    [Fact]
    public void SpeakerPrefix_CarriesTheSeparatorSpanAtTheColon()
    {
        var text = "Alice @A #main: Hi";

        var separator = SpeakerAt(text, 10).PrefixSpans!.Separator;

        Assert.Equal(10 + text.IndexOf(':'), separator.Start);
        Assert.Equal(1, separator.Length); // just the ":"
    }

    [Fact]
    public void SyntheticDefaultSpeaker_HasNoPrefixSpans()
    {
        // An orphan-tag prefix recovers to a default speaker, which names no one, so it
        // carries no prefix locations.
        var speaker = Speaker("#lonely: Hi");

        Assert.IsType<DefaultSpeaker>(speaker);
        Assert.Null(speaker.PrefixSpans);
    }

    [Theory]
    [InlineData("Alice: Hello")]
    [InlineData("Alice:   Hello")]
    public void SpeechStart_LandsAfterAllPostColonWhitespace(string text)
    {
        var result = Build(text);

        Assert.True(result.Success);
        Assert.Equal(text.IndexOf("Hello", StringComparison.Ordinal), result.MatchedLength);
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
    public void TagsWithoutNameOrId_ReportsAndRecoversToDefaultSpeaker()
    {
        var diagnostics = new DiagnosticBag();

        var result = _builder.Build(ParseInputFactory.Input("#lonely: Hi"), diagnostics);

        Assert.True(result.Success);
        Assert.IsType<DefaultSpeaker>(result.MatchedValue);
        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.TagsWithoutSpeaker);
    }

    private static ParseResult<Speaker> Build(string text) => BuildAt(text, 0);

    private static ParseResult<Speaker> BuildAt(string text, int position) =>
        _builder.Build(ParseInputFactory.Input(text, position), new DiagnosticBag());

    private static Speaker Speaker(string text) => SpeakerAt(text, 0);

    private static Speaker SpeakerAt(string text, int position)
    {
        var result = BuildAt(text, position);
        Assert.True(result.Success);
        return result.MatchedValue;
    }

    private static void AssertBuildFailed(string text) => Assert.False(Build(text).Success);
}
