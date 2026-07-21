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

    private static SemanticToken AssertSingleSemanticToken(
        IEnumerable<SemanticToken> tokens, TokenKind kind) =>
        Assert.Single(tokens, token => token.Kind == kind);

    private IReadOnlyList<SemanticToken> Project(string source) =>
        [.. _projection.Project(Pipeline.Document(source), source)];
}
