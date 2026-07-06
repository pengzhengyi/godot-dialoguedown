namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// The outcome of a parse: either a successful <see cref="Match"/>, or a failure
/// carrying an optional <see cref="Error"/> with the reason.
/// </summary>
internal readonly struct ParseResult<T>
{
    private ParseResult(bool success, ParseMatch<T> match, ParseError? error)
    {
        Success = success;
        Match = match;
        Error = error;
    }

    public bool Success { get; }

    public ParseMatch<T> Match { get; }

    public ParseError? Error { get; }

    /// <summary>The parsed value. Meaningful only when <see cref="Success"/>.</summary>
    public T MatchedValue => Match.Value;

    /// <summary>How many characters were consumed. Meaningful only when <see cref="Success"/>.</summary>
    public int MatchedLength => Match.Range.Length;

    /// <summary>The consumed range. Meaningful only when <see cref="Success"/>.</summary>
    public TextRange MatchedRange => Match.Range;

    public static ParseResult<T> Ok(ParseMatch<T> match) => new(true, match, null);

    public static ParseResult<T> Fail(ParseError error) => new(false, default, error);
}
