using DialogueDown.Cli.Compilation;
using DialogueDown.Cli.Tests.Support;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class VisualizeCommandTests
{
    [Fact]
    public void Visualize_ValidScript_ReportsNotImplemented()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("visualize", script.Path);

        Assert.Equal(ExitCodes.NotImplemented, result.ExitCode);
        Assert.Contains("not implemented", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Visualize_ReliesOnTheCompiler_PassingTheScriptSource()
    {
        var source = """
            # Gallery

            Alice: Look at this.
            """;
        using var script = new TempScript(source);
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(new CompilationResult(source));
        var tester = CliTester.Create(compiler);

        var result = tester.Run("visualize", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        compiler.Received(1).Compile(source);
    }

    [Fact]
    public void Visualize_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create();

        var result = tester.Run("visualize", "does-not-exist.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("not found", result.Output, StringComparison.OrdinalIgnoreCase);
    }
}
