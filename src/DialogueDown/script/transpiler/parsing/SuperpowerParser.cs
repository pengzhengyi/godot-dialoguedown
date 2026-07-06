using Superpower;

namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Adapts a Superpower <see cref="TextParser{T}"/> into an <see cref="IParser{T}"/>,
/// so character-level grammars stay in Superpower while composing through the
/// uniform contract.
/// </summary>
internal static class SuperpowerParser
{
    public static IParser<T> Wrap<T>(TextParser<T> grammar) => new WrappedParser<T>(grammar);

    private sealed class WrappedParser<T>(TextParser<T> grammar) : IParser<T>
    {
        public ParseResult<T> Consume(ParseInput input)
        {
            var result = grammar.TryParse(input.Text);
            if (!result.HasValue)
            {
                return ParseResult<T>.Fail(new ParseError(result.ToString()));
            }

            return ParseResult<T>.Ok(
                new ParseMatch<T>(
                    result.Value,
                    new TextRange(input.Position, result.Remainder.Position.Absolute)));
        }
    }
}
