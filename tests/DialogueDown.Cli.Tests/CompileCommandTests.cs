using DialogueDown.Cli.Commands;
using DialogueDown.Cli.Compilation;
using DialogueDown.Cli.Tests.Support;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class CompileCommandTests
{
    [Fact]
    public void Compile_ValidScript_ReportsNotImplemented()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.NotImplemented, result.ExitCode);
        Assert.Contains("not implemented", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_PassesTheScriptSourceToTheCompiler()
    {
        var source = """
            # Hello

            Alice: Hi.
            """;
        using var script = new TempScript(source);
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(new CompilationResult(source));
        var tester = CliTester.Create(compiler);

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        compiler.Received(1).Compile(source);
    }

    [Fact]
    public void Compile_ParsesTheOutputOption()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "-o", "out.txt");

        var settings = Assert.IsType<CompileSettings>(result.Settings);
        Assert.Equal("out.txt", settings.Output);
    }

    [Fact]
    public void Compile_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create();

        var result = tester.Run("compile", "does-not-exist.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("not found", result.Output, StringComparison.OrdinalIgnoreCase);
    }
}
