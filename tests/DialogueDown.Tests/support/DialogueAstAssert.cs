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

        AssertTags(declaration.Tags, tags);
        return declaration;
    }

    public static PartialSpeakerDeclaration AssertPartialSpeakerDeclaration(
        Speaker actual, string id, params Tag[] tags)
    {
        var partial = Assert.IsType<PartialSpeakerDeclaration>(actual);
        Assert.Equal(id, partial.Id);
        AssertTags(partial.Tags, tags);
        return partial;
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

    private static void AssertTags(IReadOnlyList<Tag> actual, Tag[] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(actual);
            return;
        }

        // Compare by kind, name, and value — not span, which depends on the tag's
        // position in the source and is asserted separately.
        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].GetType(), actual[i].GetType());
            Assert.Equal(expected[i].Name, actual[i].Name);
            Assert.Equal(expected[i].Value, actual[i].Value);
        }
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
