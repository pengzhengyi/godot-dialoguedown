namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// The composable core of a parser: read a prefix of the input and report the value
/// with the range it consumed, or a failure with the reason. Leaves and composites
/// implement only this. Parsing a whole string, with author-facing error reporting,
/// is the separate entry-point concern of <see cref="Parser{T}"/>.
/// </summary>
internal interface IParser<T>
{
    ParseResult<T> Consume(ParseInput input);
}
