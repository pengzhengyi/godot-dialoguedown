using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MarkdigDocument = Markdig.Syntax.MarkdownDocument;
using MarkdigHeadingBlock = Markdig.Syntax.HeadingBlock;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigLiteralInline = Markdig.Syntax.Inlines.LiteralInline;
using MarkdigParagraphBlock = Markdig.Syntax.ParagraphBlock;
using MarkdigSpan = Markdig.Syntax.SourceSpan;

namespace DialogueSystem.Markdown;

/// <summary>
/// Converts a Markdig syntax tree into our own Markdown AST. Keeping this
/// translation in one place is what isolates the rest of the compiler from the
/// Markdig library.
/// </summary>
internal sealed class MarkdownAstMapper
{
    public MarkdownDocument Map(MarkdigDocument document)
    {
        var blocks = new List<MarkdownBlock>();
        foreach (var block in document)
        {
            blocks.Add(MapBlock(block));
        }

        return new MarkdownDocument(blocks);
    }

    private static MarkdownBlock MapBlock(MarkdigBlock block) => block switch
    {
        MarkdigHeadingBlock heading => MapHeading(heading),
        MarkdigParagraphBlock paragraph => MapParagraph(paragraph),
        _ => throw new NotSupportedException(
            $"Markdown block '{block.GetType().Name}' is not yet supported."),
    };

    private static Heading MapHeading(MarkdigHeadingBlock block) =>
        // A parsed heading always has an inline container (empty for a bare '##').
        new(block.Level, MapInlines(block.Inline!), MapSpan(block.Span));

    private static Paragraph MapParagraph(MarkdigParagraphBlock block) =>
        // A parsed paragraph always has inline content, so Inline is never null here.
        new(MapInlines(block.Inline!), MapSpan(block.Span));

    private static IReadOnlyList<MarkdownInline> MapInlines(MarkdigContainerInline container)
    {
        var inlines = new List<MarkdownInline>();
        foreach (var inline in container)
        {
            inlines.Add(MapInline(inline));
        }

        return inlines;
    }

    private static MarkdownInline MapInline(MarkdigInline inline) => inline switch
    {
        MarkdigLiteralInline literal => new TextInline(literal.Content.ToString(), MapSpan(literal.Span)),
        _ => throw new NotSupportedException(
            $"Markdown inline '{inline.GetType().Name}' is not yet supported."),
    };

    private static SourceSpan MapSpan(MarkdigSpan span) => new(span.Start, span.Length);
}
