using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds the front-end Markdown parser for tests in one place, so its construction is not
/// repeated across test classes, mirroring <see cref="TranspilerBuilderFactory"/>.
/// </summary>
internal static class MarkdownParserFactory
{
    public static IMarkdownParser MarkdownParser() => new MarkdigMarkdownParser();
}
