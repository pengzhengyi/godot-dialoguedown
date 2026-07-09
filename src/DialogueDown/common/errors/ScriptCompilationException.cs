namespace DialogueDown.Common.Errors;

/// <summary>
/// A fault while compiling a script. Carries the <see cref="SourceSpan"/> that
/// locates the offending text, so a message or tool can point at the exact
/// characters that caused the problem.
/// </summary>
internal abstract class ScriptCompilationException : DialogueDownException
{
    protected ScriptCompilationException(string message, SourceSpan span)
        : base(message) => Span = span;

    protected ScriptCompilationException(string message, SourceSpan span, Exception innerException)
        : base(message, innerException) => Span = span;

    public SourceSpan Span { get; }
}
