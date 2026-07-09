namespace DialogueDown.Common.Errors;

/// <summary>
/// Structurally malformed input: the text could not be understood as valid
/// syntax. Distinct from a semantic error, where the text parses but breaks a
/// meaning rule.
/// </summary>
internal abstract class SyntaxError : ScriptCompilationException
{
    protected SyntaxError(string message, SourceSpan span)
        : base(message, span)
    {
    }

    protected SyntaxError(string message, SourceSpan span, Exception innerException)
        : base(message, span, innerException)
    {
    }
}
