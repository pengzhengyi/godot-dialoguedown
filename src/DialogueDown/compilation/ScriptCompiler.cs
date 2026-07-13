using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;

namespace DialogueDown.Compilation;

/// <summary>
/// The default <see cref="IScriptCompiler"/>: it runs the stages — parse, transpile,
/// desugar, analyze — in order, threading the source through each so a later diagnostics pass
/// can point back at it, and assembles their artifacts into a <see cref="CompilationResult"/>.
/// </summary>
internal sealed class ScriptCompiler : IScriptCompiler
{
    private readonly IMarkdownParser _parser;
    private readonly IScriptTranspiler _transpiler;
    private readonly IScriptDesugarer _desugarer;
    private readonly ISemanticAnalyzer _analyzer;

    internal ScriptCompiler(
        IMarkdownParser parser,
        IScriptTranspiler transpiler,
        IScriptDesugarer desugarer,
        ISemanticAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(transpiler);
        ArgumentNullException.ThrowIfNull(desugarer);
        ArgumentNullException.ThrowIfNull(analyzer);
        _parser = parser;
        _transpiler = transpiler;
        _desugarer = desugarer;
        _analyzer = analyzer;
    }

    /// <inheritdoc />
    public CompilationResult Compile(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var markdown = _parser.Parse(source);
        var script = _transpiler.Transpile(markdown, source);
        var desugared = _desugarer.Desugar(script, source);
        var semantics = _analyzer.Analyze(desugared, source);

        // TODO(graph-build): build the flow graph from the semantic model here as that stage
        // lands; it adds one more artifact to the result.
        return new CompilationResult(source, markdown, script, desugared, semantics);
    }
}
