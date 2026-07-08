namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>A tag (<c>#name</c>) found inside a re-tokenized string.</summary>
internal sealed record TagLeaf(TagData Tag) : InlineLeaf;
