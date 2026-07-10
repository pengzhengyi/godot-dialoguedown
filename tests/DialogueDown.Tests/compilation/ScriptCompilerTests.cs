using DialogueDown.Compilation;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Transpiler;
using NSubstitute;

namespace DialogueDown.Tests.Compilation;

public sealed class ScriptCompilerTests
{
    [Fact]
    public void Compile_RunsStagesInOrderThreadingSource_AndAssemblesResult()
    {
        var source = "Alice: hi";
        var markdown = new MarkdownDocument([]);
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);

        var parser = Substitute.For<IMarkdownParser>();
        var transpiler = Substitute.For<IScriptTranspiler>();
        var desugarer = Substitute.For<IScriptDesugarer>();
        parser.Parse(source).Returns(markdown);
        transpiler.Transpile(markdown, source).Returns(script);
        desugarer.Desugar(script, source).Returns(desugared);

        var result = new ScriptCompiler(parser, transpiler, desugarer).Compile(source);

        Assert.Equal(source, result.Source);
        Assert.Same(markdown, result.Markdown);
        Assert.Same(script, result.Script);
        Assert.Same(desugared, result.Desugared);
        Received.InOrder(() =>
        {
            parser.Parse(source);
            transpiler.Transpile(markdown, source);
            desugarer.Desugar(script, source);
        });
    }

    [Fact]
    public void Compile_NullSource_Throws()
    {
        var compiler = new ScriptCompiler(
            Substitute.For<IMarkdownParser>(),
            Substitute.For<IScriptTranspiler>(),
            Substitute.For<IScriptDesugarer>());

        Assert.Throws<ArgumentNullException>(() => compiler.Compile(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_NullDependency_Throws(int nullIndex)
    {
        var parser = nullIndex == 0 ? null! : Substitute.For<IMarkdownParser>();
        var transpiler = nullIndex == 1 ? null! : Substitute.For<IScriptTranspiler>();
        var desugarer = nullIndex == 2 ? null! : Substitute.For<IScriptDesugarer>();

        Assert.Throws<ArgumentNullException>(
            () => new ScriptCompiler(parser, transpiler, desugarer));
    }
}
