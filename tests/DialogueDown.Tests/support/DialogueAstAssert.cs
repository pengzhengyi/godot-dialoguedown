using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for Dialogue AST shapes, so transpiler tests read at the
/// level of the dialogue domain instead of repeating type checks.
/// </summary>
internal static class DialogueAstAssert
{
    public static CustomTag AssertCustomTag(Tag actual, string name, string? value = null) =>
        AssertTag<CustomTag>(actual, name, value);

    public static ReservedTag AssertReservedTag(Tag actual, string name, string? value = null) =>
        AssertTag<ReservedTag>(actual, name, value);

    private static TTag AssertTag<TTag>(Tag actual, string name, string? value)
        where TTag : Tag
    {
        var tag = Assert.IsType<TTag>(actual);
        Assert.Equal(name, tag.Name);
        if (value is null)
        {
            Assert.Null(tag.Value);
        }
        else
        {
            Assert.Equal(value, tag.Value);
        }

        return tag;
    }
}
