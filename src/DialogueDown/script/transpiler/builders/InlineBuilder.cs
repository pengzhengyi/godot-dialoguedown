using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;
using AstLineBreak = DialogueDown.Script.Ast.LineBreak;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Walks a line's inline content into a Speech — an ordered list of
/// <see cref="InlineFragment"/>s. A piece of text is re-tokenized for embedded tags
/// and jumps and built via the leaf builder; emphasis, image alt, and link labels
/// recurse; a code span becomes a game call; a soft break becomes a line break. The
/// <see cref="InlineElements"/> set says which kinds a context allows.
/// </summary>
internal sealed class InlineBuilder(InlineLeafBuilder leafBuilder, GameCallBuilder gameCallBuilder)
{
    public IReadOnlyList<InlineFragment> Build(
        IReadOnlyList<MarkdownInline> inlines, InlineElements allowed)
    {
        var speech = new List<InlineFragment>();
        foreach (var inline in inlines)
        {
            Append(inline, allowed, speech);
        }

        return speech;
    }

    private static SpeechStyle ToStyle(EmphasisKind kind) => kind switch
    {
        EmphasisKind.Italic => SpeechStyle.Italic,
        EmphasisKind.Bold => SpeechStyle.Bold,
        EmphasisKind.Strikethrough => SpeechStyle.Strikethrough,
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "Unknown emphasis kind."),
    };

    private void Append(MarkdownInline inline, InlineElements allowed, List<InlineFragment> speech)
    {
        switch (inline)
        {
            case TextInline text:
                AppendText(text, allowed, speech);
                break;
            case EmphasisInline emphasis:
                speech.Add(new StyledText(
                    ToStyle(emphasis.Kind), Build(emphasis.Children, allowed), emphasis.Span));
                break;
            case ImageInline image:
                speech.Add(new Image(image.Source, Build(image.Alt, allowed), image.Span));
                break;
            case LinkInline link:
                speech.Add(new Link(link.Target, Build(link.Label, allowed), link.Span));
                break;
            case CodeSpanInline code:
                speech.Add(gameCallBuilder.Build(new ParseInput(code.Content, code.Span.Start), code.Span));
                break;
            case MarkdownLineBreak:
                // A soft break is kept as a display hint; hard breaks are split off earlier.
                speech.Add(new AstLineBreak(inline.Span));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(inline), inline.GetType().Name, "Unhandled inline kind.");
        }
    }

    private void AppendText(TextInline text, InlineElements allowed, List<InlineFragment> speech)
    {
        var input = new ParseInput(text.Text, text.Span.Start);
        var leaves = InlineLeafTokenizer.Tokenize(input, allowJumps: allowed.HasFlag(InlineElements.Jump));
        foreach (var leaf in leaves)
        {
            speech.Add(leafBuilder.Build(leaf));
        }
    }
}
