using DialogueDown.Common;
using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Object Mother for the diagnostic model, so a test builds a descriptor or a diagnostic through
/// one place with sane defaults and a constructor change touches only this file. A descriptor is
/// built directly where it is the type under test; here it is a ready dependency for diagnostics.
/// </summary>
internal static class DiagnosticsFactory
{
    public static DiagnosticDescriptor Descriptor(
        string code = "DLG1001",
        DiagnosticCategory category = DiagnosticCategory.Syntax,
        string title = "Sample diagnostic",
        string messageFormat = "Sample message '{0}'.",
        DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error) =>
        new(code, title, messageFormat, category, defaultSeverity);

    public static Diagnostic Diagnostic(
        DiagnosticDescriptor? descriptor = null,
        SourceSpan? span = null,
        IReadOnlyList<object>? messageArguments = null,
        DiagnosticSeverity? severity = null) =>
        new(
            descriptor ?? Descriptor(),
            span ?? SourceSpanFactory.Span(),
            messageArguments ?? [],
            severity);
}
