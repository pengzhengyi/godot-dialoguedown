using DialogueDown.Common;

namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Composable building blocks for parsers. Each is an extension on
/// <see cref="IParser{T}"/> that returns a new parser, so grammars compose through
/// LINQ query syntax and fluent calls while positions thread automatically.
/// </summary>
internal static class ParserCombinators
{
    /// <summary>Transforms a parser's value, consuming exactly the same input.</summary>
    public static IParser<TResult> Select<T, TResult>(
        this IParser<T> parser, Func<T, TResult> selector) =>
        new SelectParser<T, TResult>(parser, (value, _) => selector(value));

    /// <summary>
    /// Transforms a parser's value with access to the <see cref="SourceSpan"/> it
    /// consumed, for building an AST node that carries its own span.
    /// </summary>
    public static IParser<TResult> Select<T, TResult>(
        this IParser<T> parser, Func<T, SourceSpan, TResult> selector) =>
        new SelectParser<T, TResult>(parser, (value, range) => selector(value, range.ToSourceSpan()));

    private sealed class SelectParser<T, TResult>(
        IParser<T> inner, Func<T, TextRange, TResult> project) : IParser<TResult>
    {
        public ParseResult<TResult> Consume(ParseInput input)
        {
            var result = inner.Consume(input);
            if (result.Error is { } error)
            {
                return ParseResult<TResult>.Fail(error);
            }

            return ParseResult<TResult>.Ok(
                new ParseMatch<TResult>(
                    project(result.MatchedValue, result.MatchedRange), result.MatchedRange));
        }
    }
}
