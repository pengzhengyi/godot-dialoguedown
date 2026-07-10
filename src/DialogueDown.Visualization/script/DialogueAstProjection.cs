using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization;

/// <summary>
/// Projects the Dialogue AST for display: it labels every node type, yields each node's
/// children, and attaches the original source text each node was produced from (sliced
/// from the node's span). Like the Markdown AST, its families share no single base usable
/// as the node type (<see cref="ScriptDocument"/> is a plain container; blocks, speakers,
/// inlines, and tags are separate <see cref="ScriptNode"/> families), so the node type is
/// <see cref="object"/> and each node is matched by its runtime type.
/// </summary>
internal sealed class DialogueAstProjection : INodeProjection<object>
{
    // Semantic categories: the same cross-stage vocabulary the Markdown projection uses,
    // so corresponding concepts share a color (a code span and the game call it becomes
    // are both "call"). "tag" is new — the Dialogue AST is the first stage with tags.
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
    private const string TagCategory = "tag";

    private readonly string _source;

    public DialogueAstProjection(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public string Title => "Dialogue AST";

    public string Description =>
        "The dialogue tree the transpiler derives from the Markdown — its lines and " +
        "speakers, choices, styled text, links, game calls, and tags, each tied to the " +
        "text it came from.";

    public NodeDescription Describe(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            ScriptDocument => new("Document", source: _source, category: DocumentCategory),
            SceneHeading heading => new(
                $"Scene heading (H{heading.Level})",
                [new("level", $"{heading.Level}"), SpanAttribute(heading.Span)],
                Slice(heading.Span),
                StructureCategory),
            Line line => new("Line", [SpanAttribute(line.Span)], Slice(line.Span), SpeechCategory),
            Choices choices => new(
                choices.IsOrdered ? "Choices (ordered)" : "Choices (unordered)",
                [SpanAttribute(choices.Span)],
                Slice(choices.Span),
                ChoiceCategory),
            Choice choice => new("Choice", [SpanAttribute(choice.Span)], Slice(choice.Span), ChoiceCategory),
            SpeakerDeclaration speaker => new(
                "Speaker (declaration)",
                [new("name", speaker.Name), .. Optional("id", speaker.Id), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            PartialSpeakerDeclaration speaker => new(
                "Speaker (partial)",
                [new("id", speaker.Id), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            SpeakerNameReference speaker => new(
                "Speaker (by name)",
                [new("name", speaker.Name), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            SpeakerIdReference speaker => new(
                "Speaker (by id)",
                [new("id", speaker.Id), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            Text text => new(
                "Text", [new("text", text.Content), SpanAttribute(text.Span)], Slice(text.Span), TextCategory),
            StyledText styled => new(
                $"Styled text ({styled.Style})", [SpanAttribute(styled.Span)], Slice(styled.Span), StylingCategory),
            Link link => new(
                "Link",
                [new("target", link.Target), new("label", InlineText(link.Label)), SpanAttribute(link.Span)],
                Slice(link.Span),
                JumpCategory),
            Image image => new(
                "Image",
                [new("source", image.Source), new("alt", InlineText(image.Alt)), SpanAttribute(image.Span)],
                Slice(image.Span),
                MediaCategory),
            DefaultCommand command => new(
                "Command (default)",
                [new("action", command.Action), SpanAttribute(command.Span)],
                Slice(command.Span),
                CallCategory),
            CustomCommand command => new(
                $"Command ({command.Name})",
                [new("name", command.Name), new("args", string.Join(", ", command.Args)), SpanAttribute(command.Span)],
                Slice(command.Span),
                CallCategory),
            Query query => new(
                "Query", [new("key", query.Key), SpanAttribute(query.Span)], Slice(query.Span), CallCategory),
            JumpIndicator jump => new("Jump", [SpanAttribute(jump.Span)], Slice(jump.Span), JumpCategory),
            LineBreak lineBreak => new(
                "Line break", [SpanAttribute(lineBreak.Span)], Slice(lineBreak.Span), BreakCategory),
            ReservedTag tag => new(
                "Tag (reserved)",
                [new("name", tag.Name), .. Optional("value", tag.Value), SpanAttribute(tag.Span)],
                Slice(tag.Span),
                TagCategory),
            CustomTag tag => new(
                "Tag (custom)",
                [new("name", tag.Name), .. Optional("value", tag.Value), SpanAttribute(tag.Span)],
                Slice(tag.Span),
                TagCategory),
            _ => throw new ArgumentException(
                $"Unsupported Dialogue AST node type '{node.GetType().Name}'.", nameof(node)),
        };
    }

    public IEnumerable<object> Neighbors(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            ScriptDocument document => document.Body,
            SceneHeading heading => heading.Title,
            Line line => LineChildren(line.Speaker, line.Speech),
            Choices choices => choices.Options,
            Choice choice => choice.Body,
            SpeakerDeclaration speaker => speaker.Tags,
            PartialSpeakerDeclaration speaker => speaker.Tags,
            StyledText styled => styled.Children,
            Link link => link.Label,
            Image image => image.Alt,
            _ => [],
        };
    }

    private static DisplayAttribute SpanAttribute(SourceSpan span) =>
        new("span", $"[{span.Start}, {span.End})");

    // A nullable attribute (a speaker's id, a tag's value) is shown only when present.
    private static IEnumerable<DisplayAttribute> Optional(string name, string? value) =>
        value is null ? [] : [new DisplayAttribute(name, value)];

    // A line's children are its optional speaker followed by its speech fragments.
    private static IEnumerable<object> LineChildren(Speaker? speaker, IReadOnlyList<InlineFragment> speech)
    {
        if (speaker is not null)
        {
            yield return speaker;
        }

        foreach (var fragment in speech)
        {
            yield return fragment;
        }
    }

    // A link label or image alt is a run of inline fragments; flatten it to plain text for
    // the attribute display (the node's own span still points at the exact source).
    private static string InlineText(IReadOnlyList<InlineFragment> fragments) =>
        string.Concat(fragments.Select(InlineText));

    private static string InlineText(InlineFragment fragment) => fragment switch
    {
        Text text => text.Content,
        StyledText styled => InlineText(styled.Children),
        Link link => InlineText(link.Label),
        Image image => InlineText(image.Alt),
        LineBreak => " ",
        _ => string.Empty,
    };

    // Spans come from the Markdown source locations; clamp defensively so a diagnostics
    // view never throws on a stray span.
    private string Slice(SourceSpan span)
    {
        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return _source[start..end];
    }
}
