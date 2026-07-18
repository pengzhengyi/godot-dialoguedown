using DialogueDown.Diagnostics;
using DialogueDown.Diagnostics.Errors;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Diagnostics;

public sealed class FailFastDiagnosticSinkTests
{
    [Fact]
    public void Constructor_NullInner_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new FailFastDiagnosticSink(null!));

    [Fact]
    public void Report_NullDiagnostic_Throws() =>
        Assert.Throws<ArgumentNullException>(() => FailFastSink(out _).Report(null!));

    [Fact]
    public void Report_AWarning_ForwardsToInnerWithoutThrowing()
    {
        var sink = FailFastSink(out var collected);
        var warning = Diagnostic(severity: DiagnosticSeverity.Warning);

        sink.Report(warning);

        Assert.Equal([warning], collected.Diagnostics);
    }

    [Fact]
    public void Report_AnError_ThrowsCarryingTheDiagnostic()
    {
        var error = Diagnostic(severity: DiagnosticSeverity.Error);

        var thrown = Assert.Throws<DiagnosticException>(() => FailFastSink(out _).Report(error));

        Assert.Same(error, thrown.Diagnostic);
    }

    [Fact]
    public void Report_AnError_StillForwardsItToTheInnerSinkBeforeThrowing()
    {
        var sink = FailFastSink(out var collected);
        var error = Diagnostic(severity: DiagnosticSeverity.Error);

        Assert.Throws<DiagnosticException>(() => sink.Report(error));

        Assert.Equal([error], collected.Diagnostics);
    }
}
