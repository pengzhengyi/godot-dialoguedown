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
    public void UnknownBlockKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build([new UnknownMarkdownBlock(Span())]));
}
