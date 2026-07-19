using DialogueDown.Cli.Commands;
using DialogueDown.Cli.Tests.Support;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class CompileCommandTests
{
    private const string NarratorConfig = """
        [[speakers]]
        name = "Narrator"
        default = true
        """;

    [Fact]
    public void Compile_ValidScript_Succeeds()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
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
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        var tester = CliTester.Create(compiler);

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        compiler.Received(1).Compile(source);
    }

    [Fact]
    public void Compile_WithConfig_BuildsTheCompilerFromTheResolvedOptions()
    {
        using var dir = new TempDir();
        var configPath = dir.Write("dialogue.toml", NarratorConfig);
        using var script = new TempScript("# Scene");
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        var factory = Substitute.For<Func<CompilerOptions, IScriptCompiler>>();
        factory(Arg.Any<CompilerOptions>()).Returns(compiler);
        var tester = CliTester.Create(compilerFactory: factory);

        var result = tester.Run("compile", script.Path, "--config", configPath);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        factory.Received(1).Invoke(
            Arg.Is<CompilerOptions>(o => o.Speakers.Any(s => s.Name == "Narrator")));
        compiler.Received(1).Compile(Arg.Any<string>());
    }

    [Fact]
    public void Compile_ScriptWithAnError_RendersErrataAndReturnsDataError()
    {
        using var script = new TempScript("#lonely: Hi"); // tags without a speaker → DLG1101
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.Contains("DLG1101", result.Output, StringComparison.Ordinal);
        Assert.Contains("error", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_ScriptWithOnlyAWarning_RendersErrataButSucceeds()
    {
        var source = """
            # A

            # B

            Alice: Go => [A](#a) => [B](#b)
            """;
        using var script = new TempScript(source);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("DLG1003", result.Output, StringComparison.Ordinal);
        Assert.Contains("warning", result.Output, StringComparison.Ordinal);
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
    public void Compile_MissingConfig_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--config", "no-such.toml");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("not found", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_MalformedConfig_FailsWithALocatedError()
    {
        using var dir = new TempDir();
        var configPath = dir.Write("dialogue.toml", "broken =");
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--config", configPath);

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.Contains("dialogue.toml", result.Output, StringComparison.OrdinalIgnoreCase);
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
