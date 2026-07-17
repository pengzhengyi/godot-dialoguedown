using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticSeverityTests
{
    [Fact]
    public void Order_RanksErrorAboveWarningAboveInfo()
    {
        Assert.True(DiagnosticSeverity.Error > DiagnosticSeverity.Warning);
        Assert.True(DiagnosticSeverity.Warning > DiagnosticSeverity.Info);
    }
}
