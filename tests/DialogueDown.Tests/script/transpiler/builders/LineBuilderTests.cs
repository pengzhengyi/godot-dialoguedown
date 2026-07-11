using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Errors;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;
using EmphasisKind = DialogueDown.Markdown.EmphasisKind;
using TextInline = DialogueDown.Markdown.TextInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class LineBuilderTests
{
    private readonly LineBuilder _builder = TranspilerBuilderFactory.LineBuilder();

    [Fact]
    public void WithoutASpeaker_IsPlainSpeech()
    {
        var line = _builder.Build([Text("Hello there.")]);

        Assert.Null(line.Speaker);
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void WithASpeaker_SplitsSpeakerFromSpeech()
    {
        var line = _builder.Build([Text("Alice: Hello there.")]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void SpeakerFollowedByMoreInlines_KeepsThemAllInSpeech()
    {
        var line = _builder.Build(
            [Text("Alice: I said "), Emphasis(EmphasisKind.Italic, Text("hi"))]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(2, line.Speech.Count);
        AssertText(line.Speech[0], "I said ");
        AssertStyledText(line.Speech[1], SpeechStyle.Italic);
    }

    [Fact]
    public void SpeakerWithNoSpeech_IsALineWithEmptySpeech()
    {
        var line = _builder.Build([Text("Alice:")]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Empty(line.Speech);
    }

    [Fact]
    public void LeadingNonText_HasNoSpeaker()
    {
        var line = _builder.Build([Emphasis(EmphasisKind.Bold, Text("Bang!"))]);

        Assert.Null(line.Speaker);
        AssertStyledText(Assert.Single(line.Speech), SpeechStyle.Bold);
    }

    [Fact]
    public void TagsWithoutASpeakerName_Throws() =>
        Assert.Throws<DialogueSyntaxError>(() => _builder.Build([Text("#lonely: Hi")]));

    [Fact]
    public void StyledLeadingText_IsNotASpeaker()
    {
        // `*Alice*:` parses as emphasis then ": ..." — the same shape as legitimate
        // emphasis-led prose (`*Note*: important`), so it is spoken, not a speaker.
        var line = _builder.Build(
            [Emphasis(EmphasisKind.Italic, Text("Alice")), Text(": Hello")]);

        Assert.Null(line.Speaker);
        AssertStyledText(line.Speech[0], SpeechStyle.Italic);
        AssertText(line.Speech[1], ": Hello");
    }

    [Fact]
    public void EscapedLeadingText_AnchorsSpeakerAndSpeechAtTheContentSpan()
    {
        // Mimics a leading literal whose backslash was stripped: the raw span starts at 0
        // (counting the '\'), but the content "Alice: hi" starts at 1. The speaker and the
        // leftover speech must anchor from the content, not the raw span.
        var leading = new TextInline("Alice: hi", Span(0, 10), Span(1, 9));

        var line = _builder.Build([leading]);

        var speaker = AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(1, speaker.Span.Start); // "Alice" starts at the content, not at 0
        Assert.Equal(8, AssertText(Assert.Single(line.Speech), "hi").Span.Start); // 1 + "Alice: "
    }

    [Fact]
    public void EmptyGroup_Throws() =>
        Assert.Throws<ArgumentException>(() => _builder.Build([]));

    [Fact]
    public void Span_CoversTheWholeGroup()
    {
        var line = _builder.Build(
            [new TextInline("Hi ", Span(0, 3)), new TextInline("there", Span(3, 5))]);

        Assert.Equal(0, line.Span.Start);
        Assert.Equal(8, line.Span.End);
    }
}
