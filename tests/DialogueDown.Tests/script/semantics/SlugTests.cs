using DialogueDown.Script.Semantics;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SlugTests
{
    [Theory]
    [InlineData("Play tennis", "play-tennis")]
    [InlineData("Discuss Bob's photo", "discuss-bobs-photo")]
    [InlineData("Discuss Christina's painting", "discuss-christinas-painting")]
    [InlineData("Play Tennis!", "play-tennis")]
    [InlineData("Chapter 2", "chapter-2")]
    public void From_SlugsHeadingTextLikeGitHub(string text, string expected) =>
        Assert.Equal(expected, Slug.From(text));

    [Fact]
    public void From_Lowercases() => Assert.Equal("play", Slug.From("PLAY"));

    [Fact]
    public void From_KeepsUnicodeLetters() => Assert.Equal("café", Slug.From("Café"));

    [Fact]
    public void From_KeepsUnderscoresAndExistingHyphens() =>
        Assert.Equal("pre-existing_id", Slug.From("pre-existing_id"));

    [Fact]
    public void From_DropsTheCurlyApostrophe() =>
        Assert.Equal("bobs", Slug.From("Bob\u2019s"));

    [Fact]
    public void From_TurnsEachSpaceIntoAHyphen_WithoutCollapsingRuns() =>
        Assert.Equal("a--b", Slug.From("a  b"));

    [Fact]
    public void From_DoesNotTrimEdgeSpaces() => Assert.Equal("-hi-", Slug.From(" hi "));

    [Fact]
    public void From_AllPunctuation_IsEmpty() => Assert.Equal(string.Empty, Slug.From("!!!"));
}
