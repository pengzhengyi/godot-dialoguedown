namespace DialogueDown.Common.Errors;

/// <summary>
/// A meaning rule was broken: the text parsed as valid syntax but violates a
/// semantic rule — an unresolved reference, a conflicting declaration, and so on.
/// Distinct from a <see cref="SyntaxError"/>, where the text is structurally
/// malformed.
/// </summary>
internal abstract class SemanticError : ScriptCompilationException
{
    protected SemanticError(string message, SourceSpan span)
        : base(message, span)
    {
    }

    protected SemanticError(string message, SourceSpan span, Exception innerException)
        : base(message, span, innerException)
    {
    }
}
