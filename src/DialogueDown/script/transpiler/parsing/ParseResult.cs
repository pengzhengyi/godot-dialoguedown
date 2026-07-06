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

    public static ParseResult<T> Ok(ParseMatch<T> match) => new(true, match, null);

    public static ParseResult<T> Fail(ParseError error) => new(false, default, error);
}
