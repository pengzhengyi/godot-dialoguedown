using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class InlineLeafBuilderTests
{
    private static readonly InlineLeafBuilder _builder =
        TranspilerBuilderFactory.InlineLeafBuilder();

    [Fact]
    public void Build_TextLeaf_BecomesTextWithTheRangeSpan()
    {
        var text = AssertText(AssertBuilds(new TextLeaf("hi"), new TextRange(2, 2)), "hi");

        Assert.Equal(2, text.Span.Start);
        Assert.Equal(2, text.Span.Length);
    }

    [Fact]
    public void Build_TagLeaf_BecomesATagNode()
    {
        AssertCustomTag(
            AssertBuilds(new TagLeaf(new TagData(false, "happy", null)), new TextRange(0, 6)),
            "happy");
    }

    [Fact]
    public void Build_JumpLeaf_BecomesAJumpIndicator() =>
        AssertJumpIndicator(AssertBuilds(new JumpLeaf(), new TextRange(0, 2)));

    [Fact]
    public void Build_UnknownLeaf_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build(new Spanned<InlineLeaf>(new UnknownLeaf(), new TextRange(0, 1))));

    private static SpeechFragment AssertBuilds(InlineLeaf leaf, TextRange? range = null) =>
        _builder.Build(new Spanned<InlineLeaf>(leaf, range ?? new TextRange(0, 1)));

    private sealed record UnknownLeaf : InlineLeaf;
}
