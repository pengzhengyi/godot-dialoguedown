namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfigurationSourceLocationTests
{
    [Fact]
    public void ToString_FormatsAsSourceLineColumn()
    {
        var location = new ConfigurationSourceLocation("dialogue.toml", 3, 7);

        Assert.Equal("dialogue.toml(3,7)", location.ToString());
    }
}
