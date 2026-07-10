using DialogueDown.Cli.Tests.Support;
using DialogueDown.Visualization;
using DialogueDown.Visualization.Live;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class VisualizeCommandTests
{
    [Fact]
    public void Visualize_NoArguments_OpensLauncherAtCurrentDirectoryInView()
    {
        var launcher = Launcher();
        var tester = CliTester.Create(launcher: launcher);

        var result = tester.Run("visualize");

        Assert.Equal(0, result.ExitCode);
        launcher.Received(1).RunAsync(
            Directory.GetCurrentDirectory(), null, LaunchMode.View,
            null, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_ScriptOnly_OpensAServedViewSession()
    {
        using var script = new TempScript("# Scene");
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", script.Path);

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunServedAsync(
            script.Path, null, false, null, VisualizationMode.View, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_ScriptWithEdit_OpensAServedEditSession()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(script.Path)!;
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", script.Path, "--edit", "--root", root, "--port", "5199");

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunServedAsync(
            script.Path, 5199, false, root, VisualizationMode.Edit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_EditWithoutRoot_StillOpensAServedEditSession()
    {
        using var script = new TempScript("# Scene");
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "--edit");

        runner.Received(1).RunServedAsync(
            script.Path, null, false, null, VisualizationMode.Edit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_Export_WritesAStaticReport()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "-o", "out.html", "--no-open");

        runner.Received(1).RunStatic(script.Path, "out.html", true);
        runner.DidNotReceive().RunServedAsync(
            Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_Pick_OpensTheLauncherEvenWithAScript()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(Path.GetFullPath(script.Path))!;
        var runner = Runner();
        var launcher = Launcher();
        var tester = CliTester.Create(runner: runner, launcher: launcher);

        tester.Run("visualize", script.Path, "--pick");

        runner.DidNotReceive().RunServedAsync(
            Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        launcher.Received(1).RunAsync(
            root, Path.GetFileName(script.Path), LaunchMode.View,
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

    private static IVisualizeRunner Runner()
    {
        var runner = Substitute.For<IVisualizeRunner>();
        runner
            .RunServedAsync(
                Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return runner;
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
