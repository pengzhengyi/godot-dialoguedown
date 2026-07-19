using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for reported diagnostics, so a producer test reads at the level of "a DLG code
/// was reported" instead of repeating the descriptor lookup.
/// </summary>
internal static class DiagnosticsAssert
{
    /// <summary>
    /// Asserts exactly one diagnostic matching <paramref name="descriptor"/> was reported,
    /// returning it so a test can make further assertions on its severity, span, or arguments.
    /// Prefer this overload: naming the catalog descriptor reads better than a bare DLG code.
    /// </summary>
    public static Diagnostic AssertReported(
        IReadOnlyList<Diagnostic> diagnostics, DiagnosticDescriptor descriptor) =>
        AssertReported(diagnostics, descriptor.Code);

    /// <summary>
    /// Asserts exactly one diagnostic with <paramref name="code"/> was reported, returning it so a
    /// test can make further assertions on its severity, span, or arguments.
    /// </summary>
    public static Diagnostic AssertReported(IReadOnlyList<Diagnostic> diagnostics, string code) =>
        Assert.Single(diagnostics, diagnostic => diagnostic.Descriptor.Code == code);
}
