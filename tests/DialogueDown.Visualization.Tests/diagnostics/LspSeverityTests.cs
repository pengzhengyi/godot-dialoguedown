using DialogueDown.Visualization.Diagnostics;

namespace DialogueDown.Visualization.Tests.Diagnostics;

public sealed class LspSeverityTests
{
    [Fact]
    public void Members_UseTheLspProtocolNumbers()
    {
        Assert.Equal(1, (int)LspSeverity.Error);
        Assert.Equal(2, (int)LspSeverity.Warning);
        Assert.Equal(3, (int)LspSeverity.Information);
        Assert.Equal(4, (int)LspSeverity.Hint);
    }
}
