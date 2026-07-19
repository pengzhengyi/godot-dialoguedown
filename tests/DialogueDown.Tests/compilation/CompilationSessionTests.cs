using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Diagnostics.Errors;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationSessionTests
{
    [Fact]
    public void Start_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => CompilationSession.Start(null!, CompilationMode.StageBoundary));

    [Fact]
    public void Context_CarriesTheSource() =>
        Assert.Equal(
            "Alice: hi",
            CompilationSession.Start("Alice: hi", CompilationMode.StageBoundary).Context.Source);

    [Fact]
    public void Diagnostics_ExposesWhatWasReported()
    {
        var session = CompilationSession.Start("x", CompilationMode.BestEffort);

        var warning = Report(session, DiagnosticSeverity.Warning);

        Assert.Equal([warning], session.Diagnostics);
    }

    [Fact]
    public void FailFast_ReportingAnError_ThrowsThroughTheSink()
    {
        var session = CompilationSession.Start("x", CompilationMode.FailFast);

        Assert.Throws<DiagnosticException>(() => Report(session, DiagnosticSeverity.Error));
    }

    [Theory]
    [InlineData(CompilationMode.StageBoundary, true)]
    [InlineData(CompilationMode.BestEffort, false)]
    public void ShouldHalt_AfterAnError_DependsOnMode(CompilationMode mode, bool expected)
    {
        var session = CompilationSession.Start("x", mode);

        Report(session, DiagnosticSeverity.Error);

        Assert.Equal(expected, session.ShouldHalt);
    }

    [Fact]
    public void ShouldHalt_WithOnlyAWarning_IsFalse()
    {
        var session = CompilationSession.Start("x", CompilationMode.StageBoundary);

        Report(session, DiagnosticSeverity.Warning);

        Assert.False(session.ShouldHalt);
    }

    // Reports a diagnostic of the given severity through the session's sink, returning it so a test
    // can also assert on the exact instance that was collected.
    private static Diagnostic Report(CompilationSession session, DiagnosticSeverity severity)
    {
        var diagnostic = Diagnostic(severity: severity);
        session.Context.Diagnostics.Report(diagnostic);
        return diagnostic;
    }
}
