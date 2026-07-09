using DialogueDown.Cli.Compilation;

namespace DialogueDown.Cli.Tests;

public sealed class PendingScriptCompilerTests
{
    [Fact]
    public void Compile_IsNotImplementedYet_ThrowsWithAClearMessage()
    {
        var compiler = new PendingScriptCompiler();

        var exception = Assert.Throws<NotImplementedException>(() => compiler.Compile("# Scene"));

        Assert.Contains("not implemented", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PendingScriptCompiler().Compile(null!));
    }
}
