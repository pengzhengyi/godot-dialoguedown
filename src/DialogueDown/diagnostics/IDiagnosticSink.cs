namespace DialogueDown.Diagnostics;

/// <summary>
/// The seam a producer reports a <see cref="Diagnostic"/> into during one compilation, so a
/// producer never learns how diagnostics are stored or later surfaced.
/// </summary>
internal interface IDiagnosticSink
{
    /// <summary>
    /// Collect one diagnostic. Throws <see cref="ArgumentNullException"/> when it is <c>null</c>.
    /// </summary>
    void Report(Diagnostic diagnostic);
}
