using DialogueDown.Common;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds <see cref="SourceSpan"/> values for tests. Centralized here rather than
/// inside a single AST factory because every layer — the Markdown AST and the
/// Dialogue AST — needs spans, so one place owns their construction.
/// </summary>
internal static class SourceSpanFactory
{
    public static SourceSpan Span(int start = 0, int length = 1) => new(start, length);
}
