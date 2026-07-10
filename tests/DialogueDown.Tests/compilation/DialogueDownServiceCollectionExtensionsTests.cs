using DialogueDown.Compilation;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DialogueDown.Tests.Compilation;

public sealed class DialogueDownServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDialogueDown_ResolvesAWorkingCompiler()
    {
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        var result = provider.GetRequiredService<IScriptCompiler>().Compile("Alice: hi");

        Assert.Equal("Alice: hi", result.Source);
    }

    [Fact]
    public void AddDialogueDown_RegistersTheCompilerAsASingleton()
    {
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IScriptCompiler>(),
            provider.GetRequiredService<IScriptCompiler>());
    }

    [Fact]
    public void AddDialogueDown_KeepsAPreRegisteredStage()
    {
        // A stage registered before AddDialogueDown wins, because each default uses TryAdd.
        var transpiler = Substitute.For<IScriptTranspiler>();
        transpiler.Transpile(Arg.Any<MarkdownDocument>(), Arg.Any<string>())
            .Returns(new ScriptDocument([]));
        using var provider = new ServiceCollection()
            .AddSingleton<IScriptTranspiler>(transpiler)
            .AddDialogueDown()
            .BuildServiceProvider();

        provider.GetRequiredService<IScriptCompiler>().Compile("Alice: hi");

        transpiler.Received(1).Transpile(Arg.Any<MarkdownDocument>(), "Alice: hi");
    }

    [Fact]
    public void AddDialogueDown_NullServices_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddDialogueDown());
}
