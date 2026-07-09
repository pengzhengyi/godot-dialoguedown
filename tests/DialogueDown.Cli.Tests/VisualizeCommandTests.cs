using DialogueDown.Cli.Tests.Support;
using DialogueDown.Visualization.Live;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class VisualizeCommandTests
{
    [Fact]
    public void Visualize_Default_RendersStaticallyAndOpens()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run("visualize", script.Path);

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunStatic(script.Path, null, false);
    }

    [Fact]
    public void Visualize_OutputAndNoOpen_ArePassedToTheStaticRun()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        tester.Run("visualize", script.Path, "-o", "out.html", "--no-open");

        runner.Received(1).RunStatic(script.Path, "out.html", true);
    }

    [Fact]
    public void Visualize_Watch_DrivesTheWatchRunWithItsSettings()
    {
        using var script = new TempScript("# Scene");
        var folder = Path.GetDirectoryName(script.Path)!;
        var runner = Substitute.For<IVisualizeRunner>();
        runner
            .RunWatchAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run(
            "visualize", script.Path, "--watch", "--port", "5199", "--render-root", folder);

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunWatchAsync(script.Path, 5199, false, folder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create(runner: Substitute.For<IVisualizeRunner>());

        var result = tester.Run("visualize", "does-not-exist.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }
}

