using DialogueDown.Cli.Tests.Support;
using DialogueDown.Visualization.Live;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class VisualizeCommandTests
{
    [Fact]
    public void Visualize_NoArguments_OpensLauncherAtCurrentDirectory()
    {
        var launcher = Launcher();
        var tester = CliTester.Create(launcher: launcher);

        var result = tester.Run("visualize");

        Assert.Equal(0, result.ExitCode);
        launcher.Received(1).RunAsync(
            Directory.GetCurrentDirectory(), null, LaunchMode.Static,
            null, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_ScriptOnly_OpensLauncherPreSelectingTheScript()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(Path.GetFullPath(script.Path))!;
        var launcher = Launcher();
        var tester = CliTester.Create(launcher: launcher);

        tester.Run("visualize", script.Path);

        launcher.Received(1).RunAsync(
            root, Path.GetFileName(script.Path), LaunchMode.Static,
            null, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_Export_WritesAStaticReport()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "-o", "out.html", "--no-open");

        runner.Received(1).RunStatic(script.Path, "out.html", true);
    }

    [Fact]
    public void Visualize_FullySpecifiedWatch_BypassesToTheWatchRun()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(script.Path)!;
        var runner = Substitute.For<IVisualizeRunner>();
        runner
            .RunWatchAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", script.Path, "--mode", "watch", "--root", root, "--port", "5199");

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunWatchAsync(script.Path, 5199, false, root, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_FullySpecifiedStatic_BypassesToTheStaticRun()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(script.Path)!;
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "--mode", "static", "--root", root);

        runner.Received(1).RunStatic(script.Path, null, false);
    }

    [Fact]
    public void Visualize_Pick_OpensTheLauncherEvenWhenFullySpecified()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(script.Path)!;
        var runner = Substitute.For<IVisualizeRunner>();
        var launcher = Launcher();
        var tester = CliTester.Create(runner: runner, launcher: launcher);

        tester.Run("visualize", script.Path, "--mode", "static", "--root", root, "--pick");

        runner.DidNotReceive().RunStatic(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>());
        launcher.Received(1).RunAsync(
            root, Path.GetFileName(script.Path), LaunchMode.Static,
            null, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_WatchWithoutRoot_OpensTheLauncherInWatchMode()
    {
        using var script = new TempScript("# Scene");
        var launcher = Launcher();
        var tester = CliTester.Create(launcher: launcher);

        tester.Run("visualize", script.Path, "--watch");

        launcher.Received(1).RunAsync(
            Arg.Any<string>(), Arg.Any<string?>(), LaunchMode.Watch,
            null, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create(launcher: Launcher());

        var result = tester.Run("visualize", "does-not-exist.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }

    [Fact]
    public void Visualize_OutputWithoutScript_FailsWithUsageError()
    {
        var tester = CliTester.Create(launcher: Launcher());

        var result = tester.Run("visualize", "-o", "out.html");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }

    private static ILauncherRunner Launcher()
    {
        var launcher = Substitute.For<ILauncherRunner>();
        launcher
            .RunAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<LaunchMode>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return launcher;
    }
}
