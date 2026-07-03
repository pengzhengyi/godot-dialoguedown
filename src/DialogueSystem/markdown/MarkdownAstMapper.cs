using System.Text;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MarkdigContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MarkdigDocument = Markdig.Syntax.MarkdownDocument;
using MarkdigHeadingBlock = Markdig.Syntax.HeadingBlock;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigLinkInline = Markdig.Syntax.Inlines.LinkInline;
using MarkdigListBlock = Markdig.Syntax.ListBlock;
using MarkdigListItemBlock = Markdig.Syntax.ListItemBlock;
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
    public MarkdownDocument Map(MarkdigDocument document) => new(MapBlocks(document));

    private static IReadOnlyList<MarkdownBlock> MapBlocks(IEnumerable<MarkdigBlock> blocks)
    {
        var mapped = new List<MarkdownBlock>();
        foreach (var block in blocks)
        {
            mapped.Add(MapBlock(block));
        }

        return mapped;
    }

    private static MarkdownBlock MapBlock(MarkdigBlock block) => block switch
    {
        MarkdigHeadingBlock heading => MapHeading(heading),
        MarkdigParagraphBlock paragraph => MapParagraph(paragraph),
        MarkdigListBlock list => MapList(list),
        _ => throw new NotSupportedException(
            $"Markdown block '{block.GetType().Name}' is not yet supported."),
    };

    private static ListBlock MapList(MarkdigListBlock block)
    {
        var items = new List<ListItem>();
        foreach (var child in block)
        {
            items.Add(MapListItem((MarkdigListItemBlock)child));
        }

        return new ListBlock(block.IsOrdered, items, MapSpan(block.Span));
    }

    private static ListItem MapListItem(MarkdigListItemBlock block) =>
        new(MapBlocks(block), MapSpan(block.Span));

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
        MarkdigLinkInline link when !link.IsImage => MapLink(link),
        MarkdigCodeInline code => new CodeSpanInline(code.Content, MapSpan(code.Span)),
        _ => throw new NotSupportedException(
            $"Markdown inline '{inline.GetType().Name}' is not yet supported."),
    };

    private static LinkInline MapLink(MarkdigLinkInline link) =>
        // A parsed inline link always has a URL (empty string when omitted), never null.
        new(link.Url!, FlattenLabel(link), MapSpan(link.Span));

    private static string FlattenLabel(MarkdigContainerInline label)
    {
        var text = new StringBuilder();
        foreach (var inline in label)
        {
            if (inline is not MarkdigLiteralInline literal)
            {
                throw new NotSupportedException(
                    $"Link label contains unsupported inline '{inline.GetType().Name}'.");
            }

            text.Append(literal.Content.ToString());
        }

        return text.ToString();
    }

    private static SourceSpan MapSpan(MarkdigSpan span) => new(span.Start, span.Length);
}
