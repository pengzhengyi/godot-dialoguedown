using DialogueDown.Compilation;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationResultTests
{
    [Fact]
    public void ExposesSourceEachStageArtifactAndDiagnostics()
    {
        var markdown = new MarkdownDocument([]);
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);
        var semantics = SemanticModelFactory.Minimal(desugared);
        var diagnostic = DiagnosticsFactory.Diagnostic();

        var result = new CompilationResult(
            "source", markdown, script, desugared, semantics, [diagnostic]);

        Assert.Equal("source", result.Source);
        Assert.Same(markdown, result.Markdown);
        Assert.Same(script, result.Script);
        Assert.Same(desugared, result.Desugared);
        Assert.Same(semantics, result.Semantics);
        Assert.Equal([diagnostic], result.Diagnostics);
    }

    [Fact]
    public void HasErrors_WithAnError_IsTrue()
    {
        var result = Result(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error));

        Assert.True(result.HasErrors);
    }

    [Fact]
    public void HasErrors_WarningAndInfoOnly_IsFalse()
    {
        var result = Result(
            DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Warning),
            DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Info));

        Assert.False(result.HasErrors);
    }

    [Fact]
    public void HasErrors_NoDiagnostics_IsFalse()
    {
        Assert.False(Result().HasErrors);
    }

    [Fact]
    public void Full_IsComplete_AndExposesTheLaterArtifacts()
    {
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);
        var semantics = SemanticModelFactory.Minimal(desugared);

        var result = new CompilationResult("source", new MarkdownDocument([]), script, desugared, semantics, []);

        Assert.True(result.IsComplete);
        Assert.Same(desugared, result.Desugared);
        Assert.Same(semantics, result.Semantics);
    }

    [Fact]
    public void Partial_IsNotComplete_AndReadingALaterArtifactThrows()
    {
        var result = new CompilationResult(
            "source", new MarkdownDocument([]), new ScriptDocument([]), null, null, []);

        Assert.False(result.IsComplete);
        Assert.Throws<InvalidOperationException>(() => result.Desugared);
        Assert.Throws<InvalidOperationException>(() => result.Semantics);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void NullRequiredArgument_Throws(int nullIndex)
    {
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);

        Assert.Throws<ArgumentNullException>(() => new CompilationResult(
            nullIndex == 0 ? null! : "source",
            nullIndex == 1 ? null! : new MarkdownDocument([]),
            nullIndex == 2 ? null! : script,
            desugared,
            SemanticModelFactory.Minimal(desugared),
            nullIndex == 5 ? null! : []));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void NullOptionalArtifact_DoesNotThrow(int nullIndex)
    {
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);
        DesugaredScriptDocument? desugaredArg = nullIndex == 3 ? null : desugared;
        SemanticModel? semanticsArg = nullIndex == 4 ? null : SemanticModelFactory.Minimal(desugared);

        var exception = Record.Exception(() => new CompilationResult(
            "source", new MarkdownDocument([]), script, desugaredArg, semanticsArg, []));

        Assert.Null(exception);
    }

    private static CompilationResult Result(params Diagnostic[] diagnostics)
    {
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);
        return new CompilationResult(
            "source",
            new MarkdownDocument([]),
            script,
            desugared,
            SemanticModelFactory.Minimal(desugared),
            diagnostics);
    }
}
