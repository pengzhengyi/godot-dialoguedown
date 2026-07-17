using DialogueDown.Cli.Commands;
using DialogueDown.Cli.Tests.Support;

namespace DialogueDown.Cli.Tests;

public sealed class ConfigArgumentTests
{
    [Fact]
    public void Validate_Null_Succeeds()
    {
        Assert.True(ConfigArgument.Validate(null).Successful);
    }

    [Fact]
    public void Validate_Whitespace_Fails()
    {
        var result = ConfigArgument.Validate("   ");

        Assert.False(result.Successful);
        Assert.Contains("requires a path", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_MissingFile_Fails()
    {
        var result = ConfigArgument.Validate("no-such.toml");

        Assert.False(result.Successful);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ExistingFile_Succeeds()
    {
        using var dir = new TempDir();
        var configPath = dir.Write("dialogue.toml", "");

        Assert.True(ConfigArgument.Validate(configPath).Successful);
    }
}
