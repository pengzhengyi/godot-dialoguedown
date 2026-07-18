using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticBagTests
{
    [Fact]
    public void Bag_IsADiagnosticSink()
    {
        Assert.IsAssignableFrom<IDiagnosticSink>(new DiagnosticBag());
    }

    [Fact]
    public void Report_CollectsDiagnosticsInReportOrder()
    {
        var bag = new DiagnosticBag();
        var first = DiagnosticsFactory.Diagnostic(span: SourceSpanFactory.Span(0, 1));
        var second = DiagnosticsFactory.Diagnostic(span: SourceSpanFactory.Span(5, 2));

        bag.Report(first);
        bag.Report(second);

        Assert.Equal([first, second], bag.Diagnostics);
    }

    [Fact]
    public void Diagnostics_NewBag_IsEmpty()
    {
        Assert.Empty(new DiagnosticBag().Diagnostics);
    }

    [Fact]
    public void HasErrors_NewBag_IsFalse()
    {
        Assert.False(new DiagnosticBag().HasErrors);
    }

    [Fact]
    public void HasErrors_InfoAndWarningOnly_IsFalse()
    {
        var bag = new DiagnosticBag();
        bag.Report(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Info));
        bag.Report(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Warning));

        Assert.False(bag.HasErrors);
    }

    [Fact]
    public void HasErrors_WithAnError_IsTrue()
    {
        var bag = new DiagnosticBag();
        bag.Report(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Warning));
        bag.Report(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error));

        Assert.True(bag.HasErrors);
    }

    [Fact]
    public void Report_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DiagnosticBag().Report(null!));
    }

    [Fact]
    public void Diagnostics_SnapshotDoesNotReflectLaterReports()
    {
        var bag = new DiagnosticBag();
        bag.Report(DiagnosticsFactory.Diagnostic());
        var snapshot = bag.Diagnostics;

        bag.Report(DiagnosticsFactory.Diagnostic());

        Assert.Single(snapshot);
        Assert.Equal(2, bag.Diagnostics.Count);
    }
}
