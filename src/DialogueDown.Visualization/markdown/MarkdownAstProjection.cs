using DialogueDown.Markdown;

namespace DialogueDown.Visualization;

/// <summary>
/// Projects the Markdown AST for display: it labels every node type and yields
/// each node's children. The AST has no single common base
/// (<see cref="MarkdownDocument"/>, blocks, inlines, and list items are separate
/// families), so the node type is <see cref="object"/> and each node is matched
/// by its runtime type.
/// </summary>
internal sealed class MarkdownAstProjection : INodeProjection<object>
{
    public string Title => "Markdown AST";

    public NodeDescription Describe(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            MarkdownDocument => new("Document"),
            Heading heading => new(
                $"Heading (H{heading.Level})",
                [new("level", $"{heading.Level}"), SpanAttribute(heading.Span)]),
            Paragraph paragraph => new("Paragraph", [SpanAttribute(paragraph.Span)]),
            ListBlock list => new(
                list.IsOrdered ? "List (ordered)" : "List (unordered)",
                [SpanAttribute(list.Span)]),
            ListItem item => new("List item", [SpanAttribute(item.Span)]),
            TextInline text => new("Text", [new("text", text.Text), SpanAttribute(text.Span)]),
            LinkInline link => new(
                "Link",
                [new("target", link.Target), new("label", link.Label), SpanAttribute(link.Span)]),
            ImageInline image => new(
                "Image",
                [new("source", image.Source), new("alt", image.AltText), SpanAttribute(image.Span)]),
            CodeSpanInline code => new("Code span", [new("content", code.Content), SpanAttribute(code.Span)]),
            EmphasisInline emphasis => new($"Emphasis ({emphasis.Kind})", [SpanAttribute(emphasis.Span)]),
            LineBreak lineBreak => new(
                lineBreak.IsHard ? "Line break (hard)" : "Line break (soft)",
                [SpanAttribute(lineBreak.Span)]),
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
}
