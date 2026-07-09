using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds a speech fragment from one inline leaf the tokenizer produced: a
/// <see cref="TextLeaf"/> becomes <see cref="Text"/>, a <see cref="TagLeaf"/> a
/// <see cref="Tag"/> (via <see cref="TagBuilder"/>), and a <see cref="JumpLeaf"/> a
/// <see cref="JumpIndicator"/>. The leaf's range becomes the node's span.
/// </summary>
internal sealed class InlineLeafBuilder(TagBuilder tagBuilder)
{
    public InlineFragment Build(Spanned<InlineLeaf> leaf)
    {
        var span = leaf.Range.ToSourceSpan();
        return leaf.Value switch
        {
            TextLeaf text => new Text(text.Content, span),
            TagLeaf tag => tagBuilder.Build(tag.Tag, span),
            JumpLeaf => new JumpIndicator(span),
            _ => throw new ArgumentOutOfRangeException(
                nameof(leaf), leaf.Value.GetType().Name, "Unknown inline leaf."),
        };
    }
}
