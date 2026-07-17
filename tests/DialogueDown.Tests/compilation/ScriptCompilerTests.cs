using DialogueDown.Compilation;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using NSubstitute;
using NSubstitute.Extensions;

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
        var semantics = SemanticModelFactory.Minimal(desugared);

        var parser = Substitute<IMarkdownParser, MarkdownDocument>(markdown);
        var transpiler = Substitute<IScriptTranspiler, ScriptDocument>(script);
        var desugarer = Substitute<IScriptDesugarer, DesugaredScriptDocument>(desugared);
        var analyzer = Substitute<ISemanticAnalyzer, SemanticModel>(semantics);

        var result = new ScriptCompiler(parser, transpiler, desugarer, analyzer).Compile(source);

        Assert.Equal(source, result.Source);
        Assert.Same(markdown, result.Markdown);
        Assert.Same(script, result.Script);
        Assert.Same(desugared, result.Desugared);
        Assert.Same(semantics, result.Semantics);
        Received.InOrder(() =>
        {
            parser.Parse(source);
            transpiler.Transpile(Arg.Is(markdown), Arg.Is<DiagnosticsContext>(c => c.Source == source));
            desugarer.Desugar(Arg.Is(script), Arg.Is<DiagnosticsContext>(c => c.Source == source));
            analyzer.Analyze(Arg.Is(desugared), Arg.Is<DiagnosticsContext>(c => c.Source == source));
        });
    }

    [Fact]
    public void Compile_NullSource_Throws()
    {
        var compiler = new ScriptCompiler(
            Substitute<IMarkdownParser, MarkdownDocument>(),
            Substitute<IScriptTranspiler, ScriptDocument>(),
            Substitute<IScriptDesugarer, DesugaredScriptDocument>(),
            Substitute<ISemanticAnalyzer, SemanticModel>());

        Assert.Throws<ArgumentNullException>(() => compiler.Compile(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Constructor_NullDependency_Throws(int nullIndex)
    {
        var parser = nullIndex == 0 ? null! : Substitute<IMarkdownParser, MarkdownDocument>();
        var transpiler = nullIndex == 1 ? null! : Substitute<IScriptTranspiler, ScriptDocument>();
        var desugarer = nullIndex == 2 ? null! : Substitute<IScriptDesugarer, DesugaredScriptDocument>();
        var analyzer = nullIndex == 3 ? null! : Substitute<ISemanticAnalyzer, SemanticModel>();

        Assert.Throws<ArgumentNullException>(
            () => new ScriptCompiler(parser, transpiler, desugarer, analyzer));
    }

    // Builds a stage substitute and, when a value is given, has every member that returns
    // TReturn return it — so a test reads "a parser that yields this markdown" in one line.
    private static TTarget Substitute<TTarget, TReturn>(TReturn? value = null)
        where TTarget : class
        where TReturn : class
    {
        var substitute = NSubstitute.Substitute.For<TTarget>();
        if (value is not null)
        {
            substitute.ReturnsForAll(value);
        }

        return substitute;
    }
}
