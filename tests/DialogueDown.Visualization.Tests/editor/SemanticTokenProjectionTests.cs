using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Lsp;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Editor;

public sealed class SemanticTokenProjectionTests
{
    private readonly SemanticTokenProjection _projection = new();

    [Fact]
    public void Project_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _projection.Project(null!, "x"));

    [Fact]
    public void Project_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _projection.Project(Pipeline.Document(""), null!));

    [Fact]
    public void Project_EmptyDocument_HasNoTokens() =>
        Assert.Empty(Project(""));

    [Fact]
    public void Project_ProseLine_HasNoDialogueTokens() =>
        // A speaker-less line is plain prose; Markdown highlighting owns it.
        Assert.Empty(Project("Just some narration with no speaker."));

    [Fact]
    public void Project_SpeakerName_TokenCoversTheWholePrefix()
    {
        var source = "Alice: Hello there.";

        // The speaker token is the raw prefix span, through the colon and its trailing space.
        var token = AssertSingleSemanticToken(Project(source), TokenKind.Speaker);
        Assert.Equal("Alice: ", token.TextIn(source));
    }

    [Fact]
    public void Project_SpeakerId_TokenCoversTheWholePrefix()
    {
        var source = "@alice: Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.Speaker);
        Assert.Equal("@alice: ", token.TextIn(source));
    }

    [Fact]
    public void Project_SpeakerNameAndId_TokenCoversBothTogether()
    {
        var source = "Alice @alice: Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.Speaker);
        Assert.Equal("Alice @alice: ", token.TextIn(source));
    }

    [Fact]
    public void Project_SpeakerWithTag_SpeakerCoversTheWholePrefixAndTheTagOverlaps()
    {
        var source = "Alice @alice #happy: Hello there.";
        var tokens = Project(source);

        // The coarse speaker token spans the whole prefix, including the tag; the separate tag
        // token overlaps it and the editor layers the tag on top by decoration precedence.
        var speaker = AssertSingleSemanticToken(tokens, TokenKind.Speaker);
        Assert.Equal("Alice @alice #happy: ", speaker.TextIn(source));
        var tag = AssertSingleSemanticToken(tokens, TokenKind.CustomTag);
        Assert.Equal("#happy", tag.TextIn(source));
    }

    [Fact]
    public void Project_QuotedSpeakerName_TokenCoversTheQuotedPrefix()
    {
        var source = "\"Dr. Vale\": Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.Speaker);
        Assert.Equal("\"Dr. Vale\": ", token.TextIn(source));
    }

    [Fact]
    public void Project_OrphanTagWithNoSpeaker_HasNoSpeakerToken() =>
        // A prefix of only tags names no speaker; it recovers to a default and drops the tags,
        // so nothing dialogue-specific is left to highlight.
        Assert.DoesNotContain(Project("#lonely: Hello there."), token => token.Kind == TokenKind.Speaker);

    [Fact]
    public void Project_CustomTag_TokenIncludesTheHash()
    {
        var source = "@alice #happy: Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.CustomTag);
        Assert.Equal("#happy", token.TextIn(source));
    }

    [Fact]
    public void Project_ReservedTag_TokenIncludesTheDoubleHash()
    {
        var source = "@alice ##narrator: Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.ReservedTag);
        Assert.Equal("##narrator", token.TextIn(source));
    }

    [Fact]
    public void Project_JumpIndicator_TokenCoversTheArrow()
    {
        var source = "Alice: Onward => [next](#next)";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.JumpIndicator);
        Assert.Equal("=>", token.TextIn(source));
    }

    [Fact]
    public void Project_TokenRangeIsZeroBased()
    {
        // "Bob #wow: Hi." — the custom tag "#wow" starts at column 4 on the first line.
        var token = AssertSingleSemanticToken(Project("Bob #wow: Hi."), TokenKind.CustomTag);

        Assert.Equal(new LspPosition(0, 4), token.Range.Start);
        Assert.Equal(new LspPosition(0, 8), token.Range.End);
    }

    [Fact]
    public void Project_SpeakerInASoftWrappedParagraph_TokenLandsOnItsOwnLine()
    {
        // Regression: a speaker whose paragraph soft-wraps onto a second source line. Markdig
        // rebuilds such a paragraph's content buffer, so a buffer-relative content offset put
        // the token at the top of the file; it must sit on the speaker's own line instead.
        var source =
            """
            # Scene

            Alice: a line that
            softwraps onto a second.
            """;

        var token = AssertSingleSemanticToken(Project(source), TokenKind.Speaker);

        Assert.Equal("Alice: ", token.TextIn(source));
        Assert.Equal(2, token.Range.Start.Line); // zero-based: the third line, not the heading
    }

    private static SemanticToken AssertSingleSemanticToken(
        IEnumerable<SemanticToken> tokens, TokenKind kind) =>
        Assert.Single(tokens, token => token.Kind == kind);

    private IReadOnlyList<SemanticToken> Project(string source) =>
        [.. _projection.Project(Pipeline.Document(source), source)];
}
