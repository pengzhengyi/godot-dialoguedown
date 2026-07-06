using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class SpeakerPrefixParserTests
{
    [Fact]
    public void NameWithIdAndTags_IsADeclaration() =>
        AssertSpeakerDeclaration(Speaker("Alice @A #main: Hello"), "Alice", "A", CustomTag("main"));

    [Fact]
    public void NameWithTagOnly_IsADeclaration() =>
        AssertSpeakerDeclaration(Speaker("Alice #main: Hi"), "Alice", tags: CustomTag("main"));

    [Fact]
    public void BareName_IsANameReference() =>
        AssertSpeakerNameReference(Speaker("Alice: Hello"), "Alice");

    [Fact]
    public void QuotedName_AllowsSpaces() =>
        AssertSpeakerNameReference(Speaker("\"Old Man\": Hello"), "Old Man");

    [Fact]
    public void BareId_IsAnIdReference() =>
        AssertSpeakerIdReference(Speaker("@A: Hello"), "A");

    [Fact]
    public void Declaration_TagCarriesItsOwnAbsoluteSpan()
    {
        // The prefix begins at offset 10, so the tag's span is relative to there,
        // proving per-part spans are precise rather than the whole prefix's span.
        var text = "Alice #mood=happy: Hi";
        var baseOffset = 10;

        var prefix = SpeakerPrefixParser.TryParse(
            text, SourceSpanFactory.Span(baseOffset, text.Length));

        Assert.NotNull(prefix);
        var declaration = Assert.IsType<SpeakerDeclaration>(prefix.Speaker);
        var tag = Assert.Single(declaration.Tags);
        Assert.Equal(baseOffset + text.IndexOf('#'), tag.Span.Start);
    }

    [Fact]
    public void SpeechStart_IsJustAfterTheColon()
    {
        var text = "Alice: Hello";

        var prefix = SpeakerPrefixParser.TryParse(text, SourceSpanFactory.Span(0, text.Length));

        Assert.NotNull(prefix);
        Assert.Equal(text.IndexOf(':') + 1, prefix.SpeechStart);
    }

    [Fact]
    public void ColonInsideSpeech_IsNotASpeaker() =>
        Assert.Null(SpeakerPrefixParser.TryParse(
            "The time is 3:00", SourceSpanFactory.Span(0, 16)));

    [Fact]
    public void UnquotedMultiWordName_IsNotASpeaker() =>
        // A multi-word name must be quoted; unquoted, this is default-speaker speech.
        Assert.Null(SpeakerPrefixParser.TryParse(
            "Old Man: Hello", SourceSpanFactory.Span(0, 14)));

    [Fact]
    public void LeadingColon_IsNotASpeaker() =>
        Assert.Null(SpeakerPrefixParser.TryParse(": Hello", SourceSpanFactory.Span(0, 7)));

    [Fact]
    public void MetadataWithoutName_ThrowsDialogueSyntaxError()
    {
        var error = Assert.Throws<DialogueSyntaxError>(
            () => SpeakerPrefixParser.TryParse("@A #main: Hi", SourceSpanFactory.Span(0, 12)));

        Assert.Contains("names no speaker", error.Message);
    }

    private static Speaker Speaker(string text)
    {
        var prefix = SpeakerPrefixParser.TryParse(text, SourceSpanFactory.Span(0, text.Length));
        Assert.NotNull(prefix);
        return prefix.Speaker;
    }
}
