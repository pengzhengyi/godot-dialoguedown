using DialogueDown.Compilation;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationResultTests
{
    [Fact]
    public void ExposesSourceAndEachStageArtifact()
    {
        var markdown = new MarkdownDocument([]);
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);

        var result = new CompilationResult("source", markdown, script, desugared);

        Assert.Equal("source", result.Source);
        Assert.Same(markdown, result.Markdown);
        Assert.Same(script, result.Script);
        Assert.Same(desugared, result.Desugared);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NullArgument_Throws(int nullIndex)
    {
        var script = new ScriptDocument([]);

        Assert.Throws<ArgumentNullException>(() => new CompilationResult(
            nullIndex == 0 ? null! : "source",
            nullIndex == 1 ? null! : new MarkdownDocument([]),
            nullIndex == 2 ? null! : script,
            nullIndex == 3 ? null! : new DesugaredScriptDocument(script)));
    }
}
