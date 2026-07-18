using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticsContextTests
{
    [Fact]
    public void Constructor_ExposesSourceAndDiagnostics()
    {
        var sink = new DiagnosticBag();

        var context = new DiagnosticsContext("Alice: hi", sink);

        Assert.Equal("Alice: hi", context.Source);
        Assert.Same(sink, context.Diagnostics);
    }

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DiagnosticsContext(null!, new DiagnosticBag()));
    }

    [Fact]
    public void Constructor_NullDiagnostics_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DiagnosticsContext("x", null!));
    }
}
