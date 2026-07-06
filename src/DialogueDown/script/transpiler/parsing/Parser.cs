using DialogueDown.Common;

namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A consume-oriented parser: <see cref="Consume"/> reads a prefix of the input and
/// reports the value and how much text it consumed, or a failure with the reason.
/// Parsing a whole string is the same contract with an added end check
/// (<see cref="ParseAll"/>).
/// </summary>
internal abstract class Parser<T>
{
    public abstract ParseResult<T> Consume(ParseInput input);

    /// <summary>
    /// Parses the whole input into a value, or raises a
    /// <see cref="DialogueSyntaxError"/>. Two things can go wrong, each with its own
    /// message: the grammar can <em>reject</em> the input (reported with the
    /// technical reason), or it can match a prefix yet leave text
    /// <em>unconsumed</em>.
    /// </summary>
    public T ParseAll(ParseInput input)
    {
        var result = Consume(input);

        if (result.Error is { Detail: var reason })
        {
            throw SyntaxError(input, BuildErrorMessage(input.Text, reason));
        }

        if (result.Match.Range.Length != input.Text.Length)
        {
            throw SyntaxError(input, DescribeIncompleteMatch(input.Text));
        }

        return result.Match.Value;
    }

    /// <summary>
    /// A plain-language explanation of why the input is not valid, aimed at the
    /// person writing the script — for example <c>"foo(" is not a valid tag.</c>.
    /// Subclasses describe their own grammar.
    /// </summary>
    protected abstract string DescribeFailure(string text);

    /// <summary>
    /// The message when the grammar rejects the input: the plain-language
    /// explanation, followed by the underlying technical reason. Override to change
    /// how the two are joined.
    /// </summary>
    protected virtual string BuildErrorMessage(string text, string reason) =>
        $"{DescribeFailure(text)}\n  ↳ {reason}";

    /// <summary>
    /// The message when the grammar matches a prefix but leaves trailing text.
    /// Override for a more specific explanation.
    /// </summary>
    protected virtual string DescribeIncompleteMatch(string text) =>
        $"Cannot match the full text \"{text}\".";

    // Builds the error to raise, underlining the whole failing input. The input is
    // always non-empty here; an empty input that fails to parse is a caller error and
    // surfaces loudly through SourceSpan's positive-length guard rather than being
    // quietly widened.
    private static DialogueSyntaxError SyntaxError(ParseInput input, string message) =>
        new(message, new SourceSpan(input.Position, input.Text.Length));
}
