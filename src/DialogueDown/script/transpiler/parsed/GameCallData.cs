namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>
/// The parsed form of a game call — a <see cref="QueryData"/>, a
/// <see cref="DefaultCommandData"/>, or a <see cref="CustomCommandData"/>. Span-free;
/// the builder attaches the span and produces the AST node.
/// </summary>
internal abstract record GameCallData;
