using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Transpiler;

namespace DialogueDown.Compilation;

/// <summary>
/// The default <see cref="IScriptCompiler"/>: it runs the stages — parse, transpile,
/// desugar — in order, threading the source through each so a later diagnostics pass can
/// point back at it, and assembles their artifacts into a <see cref="CompilationResult"/>.
/// </summary>
internal sealed class ScriptCompiler : IScriptCompiler
{
    private readonly IMarkdownParser _parser;
    private readonly IScriptTranspiler _transpiler;
    private readonly IScriptDesugarer _desugarer;

    internal ScriptCompiler(
        IMarkdownParser parser, IScriptTranspiler transpiler, IScriptDesugarer desugarer)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(transpiler);
        ArgumentNullException.ThrowIfNull(desugarer);
        _parser = parser;
        _transpiler = transpiler;
        _desugarer = desugarer;
    }

    /// <inheritdoc />
    public CompilationResult Compile(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var markdown = _parser.Parse(source);
        var script = _transpiler.Transpile(markdown, source);
        var desugared = _desugarer.Desugar(script, source);

        // TODO(semantic-analysis): run semantic analysis, then the graph build, here as
        // those stages land; each adds one artifact to the result.
        return new CompilationResult(source, markdown, script, desugared);
    }
}
