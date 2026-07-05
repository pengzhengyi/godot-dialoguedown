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

    public static SpeakerDeclaration AssertSpeakerDeclaration(
        Speaker actual, string name, string? id = null, params Tag[] tags)
    {
        var declaration = Assert.IsType<SpeakerDeclaration>(actual);
        Assert.Equal(name, declaration.Name);
        if (id is null)
        {
            Assert.Null(declaration.Id);
        }
        else
        {
            Assert.Equal(id, declaration.Id);
        }

        if (tags.Length == 0)
        {
            Assert.Empty(declaration.Tags);
        }
        else
        {
            Assert.Equal(tags, declaration.Tags);
        }

        return declaration;
    }

    public static SpeakerNameReference AssertSpeakerNameReference(Speaker actual, string name)
    {
        var reference = Assert.IsType<SpeakerNameReference>(actual);
        Assert.Equal(name, reference.Name);
        return reference;
    }

    public static SpeakerIdReference AssertSpeakerIdReference(Speaker actual, string id)
    {
        var reference = Assert.IsType<SpeakerIdReference>(actual);
        Assert.Equal(id, reference.Id);
        return reference;
    }

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
