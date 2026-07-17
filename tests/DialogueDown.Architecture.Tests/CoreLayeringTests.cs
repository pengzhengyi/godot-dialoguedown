using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group B — layering inside the core. The compiler pipeline flows
/// <c>Markdown -> Script.Ast -> Desugar -> Semantics -> Transpiler -> Compilation</c>
/// atop the <c>Common</c> and <c>Graph</c> foundations. Each stage may depend only
/// on stages beneath it, so a change to a later stage never ripples backward.
/// </summary>
public sealed class CoreLayeringTests
{
    [Fact]
    public void Common_IsAFoundationLeaf_WithNoDependencyOnOtherLayers()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.Common)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.Script,
                Architecture.Graph,
                Architecture.Compilation)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Configuration_IsAFoundationLeaf_WithNoDependencyOnOtherLayers()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.Configuration)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.Script,
                Architecture.Graph,
                Architecture.Compilation)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Diagnostics_IsAFoundationLeaf_WithNoDependencyOnPipelineLayers()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.Diagnostics)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.Script,
                Architecture.Graph,
                Architecture.Compilation)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Graph_IsALeaf_WithNoDependencyOnPipelineLayers()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.Graph)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.Script,
                Architecture.Compilation)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void DialogueAst_StaysDecoupledFromMarkdownAndTranspiler()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.ScriptAst)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.Markdig,
                Architecture.ScriptTranspiler)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Desugar_DoesNotDependOn_MarkdownOrTranspiler()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.ScriptDesugar)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.ScriptTranspiler)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Semantics_DoesNotDependOn_MarkdownOrTranspiler()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.ScriptSemantics)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Markdown,
                Architecture.ScriptTranspiler)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void PipelineLayers_DoNotDependOn_TheCompilationOrchestrator()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.Markdown)
            .Or()
            .ResideInNamespace(Architecture.Graph)
            .Or()
            .ResideInNamespace(Architecture.Script)
            .ShouldNot()
            .HaveDependencyOnAny(Architecture.Compilation)
            .GetResult()
            .ShouldPass();
    }
}
