namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// The single parser contract: read a prefix of the input and report the value with
/// the range it consumed, or a failure with the reason. It is non-throwing — deciding
/// whether the whole input must be consumed, turning a failure into an author-facing
/// error, and building an AST node are all the builder's responsibilities.
/// </summary>
internal interface IParser<T>
{
    ParseResult<T> Consume(ParseInput input);
}
