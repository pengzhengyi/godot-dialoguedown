using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class BlockBuilderTests
{
    private readonly BlockBuilder _builder = TranspilerBuilderFactory.BlockBuilder();

    [Fact]
    public void EmptyDocument_HasEmptyBody() =>
        Assert.Empty(_builder.Build([]));

    [Fact]
    public void Heading_BecomesAFlatSceneHeading()
    {
        var heading = Heading(2, Text("The Cave"));

        var body = _builder.Build([heading]);

        var scene = AssertSceneHeading(Assert.Single(body), "The Cave", 2);
        Assert.Equal(heading.Span, scene.Span);
    }

    [Fact]
    public void Paragraph_BecomesALine()
    {
        var body = _builder.Build([Paragraph(Text("Alice: Hello there."))]);

        var line = AssertLine(Assert.Single(body));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void Paragraph_HardBreak_SplitsIntoOneLinePerSlice()
    {
        var body = _builder.Build(
            [Paragraph(Text("Alice: Hi"), LineBreak(hard: true), Text("Bob: Yo"))]);

        Assert.Equal(2, body.Count);
        var first = AssertLine(body[0]);
        AssertSpeakerNameReference(first.Speaker!, "Alice");
        AssertSpeechText(first, "Hi");
        var second = AssertLine(body[1]);
        AssertSpeakerNameReference(second.Speaker!, "Bob");
        AssertSpeechText(second, "Yo");
    }

    [Fact]
    public void Paragraph_SoftBreak_StaysWithinOneLine()
    {
        var body = _builder.Build(
            [Paragraph(Text("Alice: Hi"), LineBreak(hard: false), Text("there"))]);

        var line = AssertLine(Assert.Single(body));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(3, line.Speech.Count);
        AssertText(line.Speech[0], "Hi");
        AssertLineBreak(line.Speech[1]);
        AssertText(line.Speech[2], "there");
    }

    [Fact]
    public void Paragraph_EmptyGroupsAroundHardBreaks_AreDropped()
    {
        var body = _builder.Build(
        [
            Paragraph(
                LineBreak(hard: true),                        // leading break -> empty group
                Text("Alice: Hi"),
                LineBreak(hard: true),
                LineBreak(hard: true),                        // doubled break -> empty group
                Text("Bob: Yo"),
                LineBreak(hard: true)),                       // trailing break -> empty group
        ]);

        Assert.Equal(2, body.Count);
        AssertSpeakerNameReference(AssertLine(body[0]).Speaker!, "Alice");
        AssertSpeakerNameReference(AssertLine(body[1]).Speaker!, "Bob");
    }

    [Fact]
    public void Paragraph_OnlyHardBreaks_EmitsNoLine() =>
        Assert.Empty(_builder.Build([Paragraph(LineBreak(hard: true))]));

    [Fact]
    public void UnknownBlockKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build([new UnknownMarkdownBlock(Span())]));
}
