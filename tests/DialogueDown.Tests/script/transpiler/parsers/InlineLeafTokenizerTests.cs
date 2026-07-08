using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class InlineLeafTokenizerTests
{
    [Fact]
    public void Scan_PlainText_IsOneTextLeafSpanningItAll()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("Hello there"), allowJumps: true);

        var leaf = Assert.Single(leaves);
        Assert.Equal("Hello there", Assert.IsType<TextLeaf>(leaf.Value).Content);
        Assert.Equal(0, leaf.Range.Start);
        Assert.Equal(11, leaf.Range.Length);
    }

    [Fact]
    public void Scan_TextIsAnchoredAtTheInputPosition()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("hi", position: 5), allowJumps: true);

        Assert.Equal(5, Assert.Single(leaves).Range.Start);
    }

    [Fact]
    public void Scan_JumpArrow_SplitsTextAroundAJumpLeaf()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("go => there"), allowJumps: true);

        Assert.Collection(
            leaves,
            leaf => Assert.Equal("go ", Assert.IsType<TextLeaf>(leaf.Value).Content),
            leaf =>
            {
                Assert.IsType<JumpLeaf>(leaf.Value);
                Assert.Equal(3, leaf.Range.Start);
                Assert.Equal(2, leaf.Range.Length);
            },
            leaf => Assert.Equal(" there", Assert.IsType<TextLeaf>(leaf.Value).Content));
    }

    [Fact]
    public void Scan_WhenJumpsDisallowed_TheArrowStaysText()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("go => there"), allowJumps: false);

        Assert.Equal("go => there", Assert.IsType<TextLeaf>(Assert.Single(leaves).Value).Content);
    }

    [Fact]
    public void Scan_StrayHashOrEquals_StaysText()
    {
        // A '#' that starts no tag and an '=' that starts no jump are plain text.
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("a # b = c"), allowJumps: true);

        Assert.Equal("a # b = c", Assert.IsType<TextLeaf>(Assert.Single(leaves).Value).Content);
    }

    [Fact]
    public void Scan_Tag_SplitsTextAroundATagLeaf()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("mood #happy"), allowJumps: true);

        Assert.Collection(
            leaves,
            leaf => Assert.Equal("mood ", Assert.IsType<TextLeaf>(leaf.Value).Content),
            leaf =>
            {
                var tag = Assert.IsType<TagLeaf>(leaf.Value).Tag;
                Assert.Equal("happy", tag.Name);
                Assert.False(tag.IsReserved);
                Assert.Equal(5, leaf.Range.Start);
                Assert.Equal(6, leaf.Range.Length);
            });
    }

    [Fact]
    public void Scan_ReservedAndGroupTags_AreRecognized()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("##npc #mood=happy"), allowJumps: false);

        Assert.Collection(
            leaves,
            leaf => Assert.True(Assert.IsType<TagLeaf>(leaf.Value).Tag.IsReserved),
            leaf => Assert.Equal(" ", Assert.IsType<TextLeaf>(leaf.Value).Content),
            leaf =>
            {
                var tag = Assert.IsType<TagLeaf>(leaf.Value).Tag;
                Assert.Equal("mood", tag.Name);
                Assert.Equal("happy", tag.Value);
            });
    }

    [Fact]
    public void Scan_TagsAndJumpsMix_InOneString()
    {
        var leaves = InlineLeafTokenizer.Tokenize(ParseInputFactory.Input("see #here => go"), allowJumps: true);

        Assert.Collection(
            leaves,
            leaf => Assert.Equal("see ", Assert.IsType<TextLeaf>(leaf.Value).Content),
            leaf => Assert.Equal("here", Assert.IsType<TagLeaf>(leaf.Value).Tag.Name),
            leaf => Assert.Equal(" ", Assert.IsType<TextLeaf>(leaf.Value).Content),
            leaf => Assert.IsType<JumpLeaf>(leaf.Value),
            leaf => Assert.Equal(" go", Assert.IsType<TextLeaf>(leaf.Value).Content));
    }
}
