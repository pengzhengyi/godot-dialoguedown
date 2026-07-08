using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler.Parsers;

/// <summary>
/// Re-tokenizes a plain string into inline leaves: pieces of plain text
/// (<see cref="TextLeaf"/>), a tag (<see cref="TagLeaf"/>) where the writer embedded
/// <c>#tag</c>, and a jump (<see cref="JumpLeaf"/>) where they wrote <c>=&gt;</c>.
/// Markdown treats these as ordinary text, so this is where they are recognized. Tags
/// are recognized in every context; jumps only where the context allows (they are
/// dropped inside a label). Each leaf keeps the range it covered, and neighbouring text
/// is joined into one piece.
/// </summary>
internal static class InlineLeafTokenizer
{
    // As many characters as possible that start neither a tag ('#') nor a jump ('=').
    private static readonly IParser<InlineLeaf> _text = SuperpowerParser.Wrap(
        Character.ExceptIn('#', '=').AtLeastOnce()
            .Select(chars => (InlineLeaf)new TextLeaf(new string(chars))));

    private static readonly IParser<InlineLeaf> _jump = SuperpowerParser.Wrap(
        Span.EqualTo("=>").Value((InlineLeaf)new JumpLeaf()));

    private static readonly IParser<InlineLeaf> _tag =
        TagParser.Token.Select(tag => (InlineLeaf)new TagLeaf(tag));

    // A single '#' or '=' that began neither a tag nor a jump survives as plain text.
    private static readonly IParser<InlineLeaf> _stray = SuperpowerParser.Wrap(
        Character.AnyChar.Select(c => (InlineLeaf)new TextLeaf(c.ToString())));

    public static IReadOnlyList<Spanned<InlineLeaf>> Tokenize(ParseInput input, bool allowJumps)
    {
        // Tags are recognized in every context; jumps only where the context allows.
        var recognized = allowJumps ? _jump.Or(_tag) : _tag;
        var leaves = recognized.Or(_text).Or(_stray).Located().Repeated().ConsumeAll(input);
        return Coalesce(leaves.MatchedValue);
    }

    private static IReadOnlyList<Spanned<InlineLeaf>> Coalesce(
        IReadOnlyList<Spanned<InlineLeaf>> leaves)
    {
        var merged = new List<Spanned<InlineLeaf>>();
        foreach (var leaf in leaves)
        {
            if (leaf.Value is TextLeaf text
                && merged.Count > 0
                && merged[^1].Value is TextLeaf previous)
            {
                var start = merged[^1].Range.Start;
                var range = new TextRange(start, leaf.Range.End - start);
                merged[^1] = new Spanned<InlineLeaf>(new TextLeaf(previous.Content + text.Content), range);
            }
            else
            {
                merged.Add(leaf);
            }
        }

        return merged;
    }
}
