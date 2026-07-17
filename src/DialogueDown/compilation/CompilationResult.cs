using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Compilation;

/// <summary>
/// The output of one compilation: the original <see cref="Source"/>, each stage's artifact,
/// and the <see cref="Diagnostics"/> collected while compiling. The stage artifacts and the
/// diagnostics are internal — they are the compiler's own types, still under active design —
/// so tooling that has friend access (the visualization project) can project them, while a
/// public caller sees the source and a <see cref="HasErrors"/> convenience. A public
/// diagnostic view (a line/column projection) lands with the renderer.
/// </summary>
public sealed record CompilationResult
{
    internal CompilationResult(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument desugared,
        SemanticModel semantics,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(desugared);
        ArgumentNullException.ThrowIfNull(semantics);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Source = source;
        Markdown = markdown;
        Script = script;
        Desugared = desugared;
        Semantics = semantics;
        Diagnostics = diagnostics;
    }

    /// <summary>The original script text this result was compiled from.</summary>
    public string Source { get; }

    /// <summary>Whether any collected diagnostic is an error — the script is not valid.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>The parsed Markdown AST — the front-end stage's artifact.</summary>
    internal MarkdownDocument Markdown { get; }

    /// <summary>The transpiled Dialogue AST — the transpiler stage's artifact.</summary>
    internal ScriptDocument Script { get; }

    /// <summary>The desugared Dialogue AST — the desugar stage's artifact.</summary>
    internal DesugaredScriptDocument Desugared { get; }

    /// <summary>The semantic model — the semantic-analysis stage's artifact.</summary>
    internal SemanticModel Semantics { get; }

    /// <summary>The diagnostics collected while compiling, in report order.</summary>
    internal IReadOnlyList<Diagnostic> Diagnostics { get; }
}
