namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A parser that recognizes a <em>leading</em> portion of the input, leaving the
/// rest for the caller — for example a speaker prefix at the start of a line. A
/// successful <see cref="ParseResult{T}"/> reports the value and how much was
/// consumed; a failure means "no such prefix here". A prefix that is present but
/// malformed raises a <see cref="DialogueSyntaxError"/> instead.
/// </summary>
internal interface IPrefixParser<T>
{
    ParseResult<T> ParsePrefix(ParseInput input);
}
