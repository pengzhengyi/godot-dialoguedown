using Bogus;
using DialogueDown.Common;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// One place to build Markdown AST nodes for tests. Each method fills in sensible
/// defaults so a test only sets what it cares about, and a constructor change
/// touches this file instead of every test. Unimportant text is faked with Bogus.
/// </summary>
internal static class MarkdownAstFactory
{
    // One Faker per test thread keeps generation safe when xUnit runs tests in
    // parallel.
    [ThreadStatic]
    private static Faker? _faker;

    private static Faker Faker => _faker ??= new Faker();

    public static SourceSpan Span(int start = 0, int length = 1) =>
        SourceSpanFactory.Span(start, length);

    public static TextInline Text(string? text = null) =>
        new(text ?? Faker.Lorem.Sentence(), Span());

    public static LinkInline Link(string? target = null, string? label = null) =>
        new(target ?? $"#{Faker.Lorem.Slug()}", label ?? Faker.Lorem.Word(), Span());

    public static CodeSpanInline CodeSpan(string? content = null) =>
        new(content ?? Faker.Lorem.Word(), Span());

    public static Heading Heading(int level = 1, params MarkdownInline[] inlines) =>
        new(level, inlines.Length == 0 ? [Text()] : inlines, Span());

    public static Paragraph Paragraph(params MarkdownInline[] inlines) =>
        new(inlines.Length == 0 ? [Text()] : inlines, Span());

    public static ListItem ListItem(params MarkdownBlock[] blocks) =>
        new(blocks.Length == 0 ? [Paragraph()] : blocks, Span());

    public static ListBlock ListBlock(bool ordered = false, params ListItem[] items) =>
        new(ordered, items.Length == 0 ? [ListItem()] : items, Span());

    public static MarkdownDocument Document(params MarkdownBlock[] blocks) =>
        new(blocks);
}
