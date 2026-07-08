using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class InlineLeafBuilderTests
{
    private static readonly InlineLeafBuilder _builder = new(new TagBuilder());

    [Fact]
    public void Build_TextLeaf_BecomesTextWithTheRangeSpan()
    {
        var text = AssertBuilds<Text>(new TextLeaf("hi"), new TextRange(2, 2));

        Assert.Equal("hi", text.Content);
        Assert.Equal(2, text.Span.Start);
        Assert.Equal(2, text.Span.Length);
    }

    [Fact]
    public void Build_TagLeaf_BecomesATagNode()
    {
        var tag = AssertBuilds<CustomTag>(
            new TagLeaf(new TagData(false, "happy", null)), new TextRange(0, 6));

        Assert.Equal("happy", tag.Name);
    }

    [Fact]
    public void Build_JumpLeaf_BecomesAJumpIndicator() =>
        AssertBuilds<JumpIndicator>(new JumpLeaf(), new TextRange(0, 2));

    [Fact]
    public void Build_UnknownLeaf_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build(new Spanned<InlineLeaf>(new UnknownLeaf(), new TextRange(0, 1))));

    private static TFragment AssertBuilds<TFragment>(
        InlineLeaf leaf,
        TextRange? range = null)
        where TFragment : SpeechFragment =>
        Assert.IsType<TFragment>(
            _builder.Build(new Spanned<InlineLeaf>(leaf, range ?? new TextRange(0, 1))));

    private sealed record UnknownLeaf : InlineLeaf;
}
