using DialogueDown.Markdown;

namespace DialogueDown.Visualization;

/// <summary>
/// Projects the Markdown AST for display: it labels every node type, yields each
/// node's children, and attaches the original source text each node was produced
/// from (sliced from the node's span). The AST has no single common base
/// (<see cref="MarkdownDocument"/>, blocks, inlines, and list items are separate
/// families), so the node type is <see cref="object"/> and each node is matched
/// by its runtime type.
/// </summary>
internal sealed class MarkdownAstProjection : INodeProjection<object>
{
    private readonly string _source;

    public MarkdownAstProjection(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public string Title => "Markdown AST";

    public NodeDescription Describe(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            MarkdownDocument => new("Document", source: _source),
            Heading heading => new(
                $"Heading (H{heading.Level})",
                [new("level", $"{heading.Level}"), SpanAttribute(heading.Span)],
                Slice(heading.Span)),
            Paragraph paragraph => new("Paragraph", [SpanAttribute(paragraph.Span)], Slice(paragraph.Span)),
            ListBlock list => new(
                list.IsOrdered ? "List (ordered)" : "List (unordered)",
                [SpanAttribute(list.Span)],
                Slice(list.Span)),
            ListItem item => new("List item", [SpanAttribute(item.Span)], Slice(item.Span)),
            TextInline text => new("Text", [new("text", text.Text), SpanAttribute(text.Span)], Slice(text.Span)),
            LinkInline link => new(
                "Link",
                [new("target", link.Target), new("label", link.Label), SpanAttribute(link.Span)],
                Slice(link.Span)),
            ImageInline image => new(
                "Image",
                [new("source", image.Source), new("alt", image.AltText), SpanAttribute(image.Span)],
                Slice(image.Span)),
            CodeSpanInline code => new(
                "Code span", [new("content", code.Content), SpanAttribute(code.Span)], Slice(code.Span)),
            EmphasisInline emphasis => new(
                $"Emphasis ({emphasis.Kind})", [SpanAttribute(emphasis.Span)], Slice(emphasis.Span)),
            LineBreak lineBreak => new(
                lineBreak.IsHard ? "Line break (hard)" : "Line break (soft)",
                [SpanAttribute(lineBreak.Span)],
                Slice(lineBreak.Span)),
            _ => throw new ArgumentException(
                $"Unsupported Markdown AST node type '{node.GetType().Name}'.", nameof(node)),
        };
    }

    public IEnumerable<object> Neighbours(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            MarkdownDocument document => document.Blocks,
            Heading heading => heading.Inlines,
            Paragraph paragraph => paragraph.Inlines,
            ListBlock list => list.Items,
            ListItem item => item.Blocks,
            EmphasisInline emphasis => emphasis.Children,
            _ => [],
        };
    }

    private static DisplayAttribute SpanAttribute(SourceSpan span) =>
        new("span", $"[{span.Start}, {span.End})");

    // Markdig source locations can occasionally run past the end of the string;
    // clamp defensively so a diagnostics view never throws on a stray span.
    private string Slice(SourceSpan span)
    {
        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return _source[start..end];
    }
}
