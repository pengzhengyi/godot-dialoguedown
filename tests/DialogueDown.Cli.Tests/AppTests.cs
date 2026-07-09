using DialogueDown.Cli.Tests.Support;

namespace DialogueDown.Cli.Tests;

public sealed class AppTests
{
    [Fact]
    public void Version_PrintsTheToolVersion()
    {
        var tester = CliTester.Create();

        var result = tester.Run("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("0.1.0", result.Output);
    }

    [Fact]
    public void Help_ListsBothCommands()
    {
        var tester = CliTester.Create();

        var result = tester.Run("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("compile", result.Output, StringComparison.Ordinal);
        Assert.Contains("visualize", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void NoArguments_ShowsHelp()
    {
        var tester = CliTester.Create();

        var result = tester.Run();

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("compile", result.Output, StringComparison.Ordinal);
        Assert.Contains("visualize", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownCommand_FailsWithUsageError()
    {
        var tester = CliTester.Create();

        var result = tester.Run("nonsense");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }
}
