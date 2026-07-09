using DialogueDown.Cli.Compilation;
using DialogueDown.Cli.Tests.Support;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DialogueDown.Cli.Tests;

public sealed class CliConfiguratorTests
{
    [Fact]
    public void UnexpectedError_IsReportedWithTheErrorExitCode()
    {
        using var script = new TempScript("# Scene");
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Throws(new InvalidOperationException("boom"));
        var tester = CliTester.Create(compiler);

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Error, result.ExitCode);
        Assert.Contains("boom", result.Output, StringComparison.Ordinal);
    }
}
