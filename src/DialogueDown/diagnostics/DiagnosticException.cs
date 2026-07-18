using DialogueDown.Common;
using DialogueDown.Common.Errors;

namespace DialogueDown.Diagnostics;

/// <summary>
/// The exception a <em>fail-fast</em> compile throws when a stage reports its first error: it
/// carries the whole <see cref="Diagnostic"/> — code, span, and message arguments — so a caller can
/// render it, rather than a bare string. The collecting modes report into the sink instead; this is
/// only how fail-fast surfaces one. Composing the human message stays a rendering concern, so the
/// exception exposes the structured diagnostic and gives itself only a terse code-and-title message.
/// </summary>
internal sealed class DiagnosticException : ScriptCompilationException
{
    public DiagnosticException(Diagnostic diagnostic)
        : base(Describe(diagnostic), SpanOf(diagnostic)) => Diagnostic = diagnostic;

    /// <summary>The diagnostic this exception carries.</summary>
    public Diagnostic Diagnostic { get; }

    private static string Describe(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return $"{diagnostic.Descriptor.Code}: {diagnostic.Descriptor.Title}";
    }

    private static SourceSpan SpanOf(Diagnostic diagnostic) => diagnostic.Span;
}
