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
    public void Compile_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() => Compiler(out _).Compile(null!));

    [Fact]
    public void Compile_NoStageReports_HasEmptyDiagnosticsAndNoErrors()
    {
        var result = Compiler(out _).Compile("Alice: hi");

        Assert.Empty(result.Diagnostics);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Compile_AStageReportsAnError_SurfacesItOnTheResultAndFlipsHasErrors()
    {
        var compiler = Compiler(out var analyzer);
        var diagnostic = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error);
        analyzer.When(a => a.Analyze(Arg.Any<DesugaredScriptDocument>(), Arg.Any<DiagnosticsContext>()))
            .Do(call => call.Arg<DiagnosticsContext>().Diagnostics.Report(diagnostic));

        var result = compiler.Compile("Alice: hi");

        Assert.Contains(diagnostic, result.Diagnostics);
        Assert.True(result.HasErrors);
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

    // Builds a compiler whose stages are substitutes that yield empty artifacts, so a test can
    // focus on the facade. Outs the analyzer so a test can install a spy that reports into the
    // context it is handed.
    private static ScriptCompiler Compiler(out ISemanticAnalyzer analyzer)
    {
        var desugared = new DesugaredScriptDocument(new ScriptDocument([]));
        analyzer = Substitute<ISemanticAnalyzer, SemanticModel>(SemanticModelFactory.Minimal(desugared));
        return new ScriptCompiler(
            Substitute<IMarkdownParser, MarkdownDocument>(new MarkdownDocument([])),
            Substitute<IScriptTranspiler, ScriptDocument>(new ScriptDocument([])),
            Substitute<IScriptDesugarer, DesugaredScriptDocument>(desugared),
            analyzer);
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
