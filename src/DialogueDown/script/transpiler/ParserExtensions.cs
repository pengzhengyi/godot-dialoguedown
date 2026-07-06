using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Reusable combinators shared across the transpiler's leaf parsers.
/// </summary>
internal static class ParserExtensions
{
    /// <summary>
    /// Requires the parsed value to be wrapped in a left and right parenthesis.
    /// The parentheses are consumed and excluded from the value.
    /// </summary>
    public static TextParser<T> EnclosedInParentheses<T>(this TextParser<T> parser) =>
        parser.Between(Character.EqualTo('('), Character.EqualTo(')'));

    /// <summary>
    /// Runs a parser and also reports the <see cref="TextSpan"/> it consumed, so a
    /// caller can turn each matched part's position into an absolute source span.
    /// </summary>
    public static TextParser<(T Value, TextSpan Location)> Located<T>(this TextParser<T> parser) =>
        input =>
        {
            var result = parser(input);
            if (!result.HasValue)
            {
                return Result.CastEmpty<T, (T, TextSpan)>(result);
            }

            var location = input.Until(result.Remainder);
            return Result.Value((result.Value, location), input, result.Remainder);
        };
}
