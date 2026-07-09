namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>
/// The base for what a plain string re-tokenizes into: a piece of plain text
/// (<see cref="TextLeaf"/>), a tag (<see cref="TagLeaf"/>), or a jump marker
/// (<see cref="JumpLeaf"/>). These are the terminal inline elements — they hold no
/// nested inlines, unlike emphasis or a link. Span-free: the tokenizer pairs each leaf
/// with its range, and the builder turns it into an AST node.
/// </summary>
internal abstract record InlineLeaf;
