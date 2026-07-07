using Superpower;
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
}
