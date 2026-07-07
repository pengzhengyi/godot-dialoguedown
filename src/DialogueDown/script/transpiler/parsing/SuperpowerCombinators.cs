using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Reusable combinators over Superpower's <see cref="TextParser{T}"/>, shared across
/// the DSL grammars.
/// </summary>
internal static class SuperpowerCombinators
{
    /// <summary>
    /// Requires the parsed value to be wrapped in a left and right parenthesis.
    /// The parentheses are consumed and excluded from the value.
    /// </summary>
    public static TextParser<T> EnclosedInParentheses<T>(this TextParser<T> parser) =>
        parser.Between(Character.EqualTo('('), Character.EqualTo(')'));
}
