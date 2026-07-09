using DialogueDown.Cli;
using Spectre.Console.Cli.Testing;

namespace DialogueDown.Cli.Tests;

public sealed class AppTests
{
    [Fact]
    public void Version_PrintsTheToolVersion()
    {
        var tester = new CommandAppTester();
        tester.Configure(CliConfigurator.Configure);

        var result = tester.Run("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("0.1.0", result.Output);
    }
}
