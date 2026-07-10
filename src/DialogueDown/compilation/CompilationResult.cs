using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Compilation;

/// <summary>
/// The output of one compilation: the original <see cref="Source"/> plus each stage's
/// artifact. The stage artifacts are internal — they are the compiler's own tree types,
/// still under active design — so tooling that has friend access (the visualization
/// project) can project them, while a public caller sees only the source. Diagnostics and
/// a compiled output are planned public additions as the later stages land.
/// </summary>
public sealed record CompilationResult
{
    internal CompilationResult(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument desugared)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(desugared);
        Source = source;
        Markdown = markdown;
        Script = script;
        Desugared = desugared;
    }

    /// <summary>The original script text this result was compiled from.</summary>
    public string Source { get; }

    /// <summary>The parsed Markdown AST — the front-end stage's artifact.</summary>
    internal MarkdownDocument Markdown { get; }

    /// <summary>The transpiled Dialogue AST — the transpiler stage's artifact.</summary>
    internal ScriptDocument Script { get; }

    /// <summary>The desugared Dialogue AST — the desugar stage's artifact.</summary>
    internal DesugaredScriptDocument Desugared { get; }
}
