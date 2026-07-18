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
    private readonly DesugaredScriptDocument? _desugared;
    private readonly SemanticModel? _semantics;

    internal CompilationResult(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument? desugared,
        SemanticModel? semantics,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Source = source;
        Markdown = markdown;
        Script = script;
        _desugared = desugared;
        _semantics = semantics;
        Diagnostics = diagnostics;
    }

    /// <summary>The original script text this result was compiled from.</summary>
    public string Source { get; }

    /// <summary>Whether any collected diagnostic is an error — the script is not valid.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.IsError);

    /// <summary>
    /// Whether the compile ran to completion. It is false when a stage-boundary compile halted
    /// after an erroring stage, so the later artifacts (<see cref="Desugared"/>,
    /// <see cref="Semantics"/>) were never produced.
    /// </summary>
    internal bool IsComplete => _semantics is not null;

    /// <summary>The parsed Markdown AST — the front-end stage's artifact.</summary>
    internal MarkdownDocument Markdown { get; }

    /// <summary>The transpiled Dialogue AST — the transpiler stage's artifact.</summary>
    internal ScriptDocument Script { get; }

    /// <summary>The desugared Dialogue AST — the desugar stage's artifact. Throws when the compile
    /// halted before desugaring; check <see cref="IsComplete"/> first.</summary>
    internal DesugaredScriptDocument Desugared => _desugared ?? throw NotProduced(nameof(Desugared));

    /// <summary>The semantic model — the semantic-analysis stage's artifact. Throws when the compile
    /// halted before analysis; check <see cref="IsComplete"/> first.</summary>
    internal SemanticModel Semantics => _semantics ?? throw NotProduced(nameof(Semantics));

    /// <summary>The diagnostics collected while compiling, in report order.</summary>
    internal IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>A result for a compile that ran to completion, carrying every stage artifact.</summary>
    internal static CompilationResult Complete(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument desugared,
        SemanticModel semantics,
        IReadOnlyList<Diagnostic> diagnostics) =>
        new(source, markdown, script, desugared, semantics, diagnostics);

    /// <summary>A result for a compile that halted after an erroring stage, without the artifacts
    /// the skipped stages would have produced.</summary>
    internal static CompilationResult Halted(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        IReadOnlyList<Diagnostic> diagnostics) =>
        new(source, markdown, script, desugared: null, semantics: null, diagnostics);

    private static InvalidOperationException NotProduced(string artifact) =>
        new($"{artifact} was not produced: the compile halted at an earlier stage. Check "
            + $"{nameof(IsComplete)} (or compile in best-effort mode) before reading it.");
}
