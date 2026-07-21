using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Editor;

/// <summary>
/// Test helper for reading the source text a <see cref="SemanticToken"/> covers, so a token
/// assertion reads as the substring it expects rather than raw offsets.
/// </summary>
internal static class SemanticTokenExtensions
{
    /// <summary>The source text this token covers, resolved from its zero-based range.</summary>
    public static string TextIn(this SemanticToken token, string source) =>
        source[token.Range.Start.OffsetIn(source)..token.Range.End.OffsetIn(source)];
}
