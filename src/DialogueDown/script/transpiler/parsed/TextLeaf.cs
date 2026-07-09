namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>A piece of plain text in a re-tokenized string.</summary>
internal sealed record TextLeaf(string Content) : InlineLeaf;
