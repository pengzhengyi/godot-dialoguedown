namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A parsed value paired with the <see cref="TextRange"/> it occupied, so a list of
/// parts (such as a speaker's tags) keeps each part's location while the parsed data
/// itself stays span-free. The builder converts the range to a
/// <see cref="DialogueDown.Common.SourceSpan"/> when it makes the node.
/// </summary>
internal readonly record struct Spanned<T>(T Value, TextRange Range);
