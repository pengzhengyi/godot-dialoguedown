using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Diagnostics;

public sealed class LocatedDiagnosticTests
{
    [Fact]
    public void Project_RendersTheMessageAndLocatesTheSpan()
    {
        // "line one\nAlice: hi": "Alice" starts at offset 9 (line 2, column 1) and spans 5 chars.
        const string source = "line one\nAlice: hi";
        var diagnostic = Diagnostic(
            descriptor: Descriptor(
                code: "DLG2003",
                category: DiagnosticCategory.Semantic,
                messageFormat: "Name '{0}' clashes."),
            span: SourceSpanFactory.Span(9, 5),
            messageArguments: ["Alice"],
            severity: DiagnosticSeverity.Error);

        var located = LocatedDiagnostic.Project(diagnostic, new LineMap(source));

        Assert.Equal("DLG2003", located.Code);
        Assert.Equal(DiagnosticSeverity.Error, located.Severity);
        Assert.Equal("Name 'Alice' clashes.", located.Message);
        Assert.Equal(new LinePosition(2, 1), located.Start);
        Assert.Equal(new LinePosition(2, 6), located.End);
    }

    [Fact]
    public void Project_KeepsTheDiagnosticsOverriddenSeverity()
    {
        var diagnostic = Diagnostic(
            descriptor: Descriptor(messageFormat: "No arguments.", defaultSeverity: DiagnosticSeverity.Error),
            severity: DiagnosticSeverity.Warning);

        var located = LocatedDiagnostic.Project(diagnostic, new LineMap("x"));

        Assert.Equal(DiagnosticSeverity.Warning, located.Severity);
    }
}
