using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for the leaves the inline tokenizer produces, so its tests read as
/// "a text leaf, then a tag" instead of repeating the type check on every leaf.
/// </summary>
internal static class InlineLeafAssert
{
    public static void AssertTextLeaf(Spanned<InlineLeaf> leaf, string content) =>
        Assert.Equal(content, Assert.IsType<TextLeaf>(leaf.Value).Content);

    public static TagData AssertTagLeaf(Spanned<InlineLeaf> leaf) =>
        Assert.IsType<TagLeaf>(leaf.Value).Tag;

    public static void AssertJumpLeaf(Spanned<InlineLeaf> leaf) =>
        Assert.IsType<JumpLeaf>(leaf.Value);

    public static void AssertRange(Spanned<InlineLeaf> leaf, int start, int length)
    {
        Assert.Equal(start, leaf.Range.Start);
        Assert.Equal(length, leaf.Range.Length);
    }
}
