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
    // Semantic categories: a stable, cross-stage vocabulary the renderer maps to
    // colours. Later stages reuse these names so corresponding concepts share a
    // colour — e.g. a code span becomes a game call, so both are "call".
    private const string DocumentCategory = "document";
    private const string StructureCategory = "structure";
    private const string SpeechCategory = "speech";
    private const string TextCategory = "text";
    private const string ChoiceCategory = "choice";
    private const string JumpCategory = "jump";
    private const string MediaCategory = "media";
    private const string CallCategory = "call";
    private const string StylingCategory = "styling";
    private const string BreakCategory = "break";

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
            MarkdownDocument => new("Document", source: _source, category: DocumentCategory),
            Heading heading => new(
                $"Heading (H{heading.Level})",
                [new("level", $"{heading.Level}"), SpanAttribute(heading.Span)],
                Slice(heading.Span),
                StructureCategory),
            Paragraph paragraph => new(
                "Paragraph", [SpanAttribute(paragraph.Span)], Slice(paragraph.Span), SpeechCategory),
            ListBlock list => new(
                list.IsOrdered ? "List (ordered)" : "List (unordered)",
                [SpanAttribute(list.Span)],
                Slice(list.Span),
                ChoiceCategory),
            ListItem item => new("List item", [SpanAttribute(item.Span)], Slice(item.Span), ChoiceCategory),
            TextInline text => new(
                "Text", [new("text", text.Text), SpanAttribute(text.Span)], Slice(text.Span), TextCategory),
            LinkInline link => new(
                "Link",
                [new("target", link.Target), new("label", link.Label), SpanAttribute(link.Span)],
                Slice(link.Span),
                JumpCategory),
            ImageInline image => new(
                "Image",
                [new("source", image.Source), new("alt", image.AltText), SpanAttribute(image.Span)],
                Slice(image.Span),
                MediaCategory),
            CodeSpanInline code => new(
                "Code span",
                [new("content", code.Content), SpanAttribute(code.Span)],
                Slice(code.Span),
                CallCategory),
            EmphasisInline emphasis => new(
                $"Emphasis ({emphasis.Kind})", [SpanAttribute(emphasis.Span)], Slice(emphasis.Span), StylingCategory),
            LineBreak lineBreak => new(
                lineBreak.IsHard ? "Line break (hard)" : "Line break (soft)",
                [SpanAttribute(lineBreak.Span)],
                Slice(lineBreak.Span),
                BreakCategory),
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
