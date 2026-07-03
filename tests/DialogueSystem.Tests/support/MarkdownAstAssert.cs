using DialogueSystem.Markdown;

namespace DialogueSystem.Tests.Support;

/// <summary>
/// Assertion helpers for common Markdown AST shapes, so parser tests stay short
/// and read at the level of "a document with one paragraph of text".
/// </summary>
internal static class MarkdownAstAssert
{
    public static TBlock AssertSingleBlock<TBlock>(MarkdownDocument document)
        where TBlock : MarkdownBlock =>
        Assert.IsType<TBlock>(Assert.Single(document.Blocks));

    public static TInline AssertSingleInline<TInline>(IReadOnlyList<MarkdownInline> inlines)
        where TInline : MarkdownInline =>
        Assert.IsType<TInline>(Assert.Single(inlines));

    public static void AssertSingleText(IReadOnlyList<MarkdownInline> inlines, string expected)
    {
        var text = AssertSingleInline<TextInline>(inlines);
        Assert.Equal(expected, text.Text);
    }
}
