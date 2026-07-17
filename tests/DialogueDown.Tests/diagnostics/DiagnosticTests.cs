using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticTests
{
    [Fact]
    public void Constructor_ExposesDescriptorSpanAndArguments()
    {
        var descriptor = DiagnosticsFactory.Descriptor();
        var span = SourceSpanFactory.Span(3, 5);

        var diagnostic = new Diagnostic(descriptor, span, ["Alice"]);

        Assert.Equal(descriptor, diagnostic.Descriptor);
        Assert.Equal(span, diagnostic.Span);
        Assert.Equal(["Alice"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Severity_DefaultsToDescriptorDefault_WhenNotOverridden()
    {
        var descriptor = DiagnosticsFactory.Descriptor(defaultSeverity: DiagnosticSeverity.Warning);

        var diagnostic = new Diagnostic(descriptor, SourceSpanFactory.Span(), []);

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void Severity_UsesOverride_WhenProvided()
    {
        var descriptor = DiagnosticsFactory.Descriptor(defaultSeverity: DiagnosticSeverity.Error);

        var diagnostic = new Diagnostic(
            descriptor, SourceSpanFactory.Span(), [], DiagnosticSeverity.Info);

        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    [Fact]
    public void Equality_SameValuesIncludingEmptyArguments_AreEqual()
    {
        var descriptor = DiagnosticsFactory.Descriptor();
        var one = new Diagnostic(descriptor, SourceSpanFactory.Span(2, 3), []);
        var two = new Diagnostic(descriptor, SourceSpanFactory.Span(2, 3), []);

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
    }

    [Fact]
    public void Equality_EqualArgumentContentInSeparateLists_AreEqual()
    {
        var descriptor = DiagnosticsFactory.Descriptor();
        var one = new Diagnostic(descriptor, SourceSpanFactory.Span(), ["Alice"]);
        var two = new Diagnostic(descriptor, SourceSpanFactory.Span(), ["Alice"]);

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentArguments_AreNotEqual()
    {
        var descriptor = DiagnosticsFactory.Descriptor();
        var one = new Diagnostic(descriptor, SourceSpanFactory.Span(), ["Alice"]);
        var two = new Diagnostic(descriptor, SourceSpanFactory.Span(), ["Bob"]);

        Assert.NotEqual(one, two);
    }

    [Fact]
    public void Equality_DifferentSeverity_AreNotEqual()
    {
        var descriptor = DiagnosticsFactory.Descriptor(defaultSeverity: DiagnosticSeverity.Error);
        var error = new Diagnostic(descriptor, SourceSpanFactory.Span(), []);
        var warning = new Diagnostic(
            descriptor, SourceSpanFactory.Span(), [], DiagnosticSeverity.Warning);

        Assert.NotEqual(error, warning);
    }
}
