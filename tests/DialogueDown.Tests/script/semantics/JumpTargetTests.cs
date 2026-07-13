using DialogueDown.Script.Semantics;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class JumpTargetTests
{
    [Fact]
    public void Parse_SameFileAnchor_HasNoFile()
    {
        var target = JumpTarget.Parse("#play-tennis");

        Assert.Null(target.File);
        Assert.Equal("play-tennis", target.Anchor);
    }

    [Fact]
    public void Parse_CrossFileWithAnchor_SplitsBoth()
    {
        var target = JumpTarget.Parse("chapter-02.md#meet-bob");

        Assert.Equal("chapter-02.md", target.File);
        Assert.Equal("meet-bob", target.Anchor);
    }

    [Fact]
    public void Parse_CrossFileWithoutAnchor_HasNoAnchor()
    {
        var target = JumpTarget.Parse("chapter-02.md");

        Assert.Equal("chapter-02.md", target.File);
        Assert.Null(target.Anchor);
    }

    [Fact]
    public void Parse_HashOnly_IsEmpty()
    {
        var target = JumpTarget.Parse("#");

        Assert.Null(target.File);
        Assert.Null(target.Anchor);
    }

    [Fact]
    public void Parse_EmptyString_IsEmpty()
    {
        var target = JumpTarget.Parse(string.Empty);

        Assert.Null(target.File);
        Assert.Null(target.Anchor);
    }

    [Fact]
    public void Parse_SplitsAtTheFirstHash()
    {
        var target = JumpTarget.Parse("a.md#sec#tion");

        Assert.Equal("a.md", target.File);
        Assert.Equal("sec#tion", target.Anchor);
    }
}
