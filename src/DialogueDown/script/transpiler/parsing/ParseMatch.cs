namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A successful parse: the produced <see cref="Value"/> and the <see cref="Range"/>
/// of source it consumed.
/// </summary>
internal readonly record struct ParseMatch<T>(T Value, TextRange Range);
