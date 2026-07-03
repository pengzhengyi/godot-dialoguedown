using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MarkdigContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MarkdigDocument = Markdig.Syntax.MarkdownDocument;
using MarkdigHeadingBlock = Markdig.Syntax.HeadingBlock;
using MarkdigHtmlBlock = Markdig.Syntax.HtmlBlock;
using MarkdigHtmlBlockType = Markdig.Syntax.HtmlBlockType;
using MarkdigHtmlInline = Markdig.Syntax.Inlines.HtmlInline;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigLineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
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
/// Markdig library. A fresh instance is created per parse and holds the source
/// text, so constructs we do not model can be sliced back to their raw text and
/// flattened rather than dropped or rejected.
/// </summary>
internal sealed class MarkdigToMarkdownAstConverter
{
    private readonly string _source;

    public MarkdigToMarkdownAstConverter(string source)
    {
        _source = source;
    }

    public MarkdownDocument Convert(MarkdigDocument document) => new(ConvertBlocks(document));

    // Single decision point for content that is dropped entirely instead of being
    // flattened to raw text. A future kind to ignore (or a configurable skip list)
    // extends here, without touching the conversion switches.
    private static bool ShouldSkip(MarkdigBlock block) => IsComment(block);

    private static bool ShouldSkip(MarkdigInline inline) => IsComment(inline);

    private static bool IsComment(MarkdigBlock block) =>
        block is MarkdigHtmlBlock html && html.Type == MarkdigHtmlBlockType.Comment;

    private static bool IsComment(MarkdigInline inline) =>
        inline is MarkdigHtmlInline html && html.Tag.StartsWith("<!--", StringComparison.Ordinal);

    private static SourceSpan ConvertSpan(MarkdigSpan span) => new(span.Start, span.Length);

    private IReadOnlyList<MarkdownBlock> ConvertBlocks(IEnumerable<MarkdigBlock> blocks)
    {
        var converted = new List<MarkdownBlock>();
        foreach (var block in blocks)
        {
            if (ShouldSkip(block))
            {
                continue;
            }

            converted.Add(ConvertBlock(block));
        }

        return converted;
    }

    private MarkdownBlock ConvertBlock(MarkdigBlock block) => block switch
    {
        MarkdigHeadingBlock heading => ConvertHeading(heading),
        MarkdigParagraphBlock paragraph => ConvertParagraph(paragraph),
        MarkdigListBlock list => ConvertList(list),
        _ => FlattenBlock(block),
    };

    private ListBlock ConvertList(MarkdigListBlock block)
    {
        var items = new List<ListItem>();
        foreach (var child in block)
        {
            items.Add(ConvertListItem((MarkdigListItemBlock)child));
        }

        return new ListBlock(block.IsOrdered, items, ConvertSpan(block.Span));
    }

    private ListItem ConvertListItem(MarkdigListItemBlock block) =>
        new(ConvertBlocks(block), ConvertSpan(block.Span));

    private Heading ConvertHeading(MarkdigHeadingBlock block) =>
        // A parsed heading always has an inline container (empty for a bare '##').
        new(block.Level, ConvertInlines(block.Inline!), ConvertSpan(block.Span));

    private Paragraph ConvertParagraph(MarkdigParagraphBlock block) =>
        // A parsed paragraph always has inline content, so Inline is never null here.
        new(ConvertInlines(block.Inline!), ConvertSpan(block.Span));

    private Paragraph FlattenBlock(MarkdigBlock block)
    {
        // A construct we do not model (blockquote, thematic break, ...) survives as
        // a paragraph of its exact source text, so nothing is silently dropped.
        var span = ConvertSpan(block.Span);
        return new Paragraph([new TextInline(Slice(block.Span), span)], span);
    }

    private IReadOnlyList<MarkdownInline> ConvertInlines(MarkdigContainerInline container)
    {
        var inlines = new List<MarkdownInline>();
        foreach (var inline in container)
        {
            if (ShouldSkip(inline))
            {
                continue;
            }

            inlines.Add(ConvertInline(inline));
        }

        return inlines;
    }

    private MarkdownInline ConvertInline(MarkdigInline inline) => inline switch
    {
        MarkdigLiteralInline literal => new TextInline(literal.Content.ToString(), ConvertSpan(literal.Span)),
        MarkdigLinkInline link when !link.IsImage => ConvertLink(link),
        MarkdigCodeInline code => new CodeSpanInline(code.Content, ConvertSpan(code.Span)),
        MarkdigLineBreakInline lineBreak => new LineBreak(lineBreak.IsHard, ConvertSpan(lineBreak.Span)),
        _ => FlattenInline(inline),
    };

    private TextInline FlattenInline(MarkdigInline inline)
    {
        // A construct we do not model (image, autolink, ...) survives as its exact
        // source text so no spoken content is lost.
        var span = ConvertSpan(inline.Span);
        return new TextInline(Slice(inline.Span), span);
    }

    private LinkInline ConvertLink(MarkdigLinkInline link) =>
        // A parsed inline link always has a URL (empty string when omitted), never null.
        new(link.Url!, FlattenLabel(link), ConvertSpan(link.Span));

    private string FlattenLabel(MarkdigLinkInline link)
    {
        // Keep the label as its raw source text; inner formatting is not treated as
        // dialogue structure.
        var first = link.FirstChild;
        if (first is null)
        {
            return string.Empty;
        }

        return Slice(new MarkdigSpan(first.Span.Start, link.LastChild!.Span.End));
    }

    private string Slice(MarkdigSpan span) => _source.Substring(span.Start, span.Length);
}
