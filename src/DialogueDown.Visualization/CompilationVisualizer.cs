using DialogueDown.Markdown;
using DialogueDown.Script.Transpiler;

namespace DialogueDown.Visualization;

/// <summary>
/// The entry facade for compilation visualization: it compiles a script string,
/// projects each available stage into a display graph, and assembles a single
/// self-contained, multi-tab HTML report. The live-visualization server consumes
/// the same facade to render and serialize a document.
/// </summary>
/// <remarks>
/// <see cref="BuildStages"/> projects two stages: the parsed Markdown AST and the
/// Dialogue AST the transpiler derives from it. Each is one projection over the shared
/// walk, model, and renderers.
/// </remarks>
public sealed class CompilationVisualizer
{
    private readonly IMarkdownParser _parser;
    private readonly IScriptTranspiler _transpiler;

    /// <summary>Creates a visualizer using the default Markdig-based parser.</summary>
    public CompilationVisualizer()
        : this(new MarkdigMarkdownParser())
    {
    }

    /// <summary>Creates a visualizer over a specific <paramref name="parser"/>.</summary>
    internal CompilationVisualizer(IMarkdownParser parser)
        : this(parser, ScriptTranspilerFactory.CreateDefault())
    {
    }

    /// <summary>Creates a visualizer over a specific parser and transpiler.</summary>
    internal CompilationVisualizer(IMarkdownParser parser, IScriptTranspiler transpiler)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(transpiler);
        _parser = parser;
        _transpiler = transpiler;
    }

    /// <summary>Compiles the source and projects each stage into a display graph.</summary>
    public IReadOnlyList<DisplayGraph> BuildStages(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var markdown = _parser.Parse(source);
        var dialogue = _transpiler.Transpile(markdown, source);
        return [markdown.ToDisplayGraph(source), dialogue.ToDisplayGraph(source)];
    }

    /// <summary>
    /// Returns the local image references in the document, in document order — the
    /// sources of <c>![alt](src)</c> images that name a file rather than a web
    /// resource. Web and data URLs (<c>http:</c>, <c>https:</c>, <c>//</c>,
    /// <c>data:</c>) are excluded. The live server uses these to decide which folder
    /// it must serve so the report's images resolve.
    /// </summary>
    public IReadOnlyList<string> LocalImageReferences(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var projection = new MarkdownAstProjection(source);
        var references = new List<string>();
        CollectLocalImages(_parser.Parse(source), projection, references);
        return references;
    }

    /// <summary>
    /// Compiles the source and renders the static, multi-tab HTML report. When
    /// <paramref name="documentPath"/> is given it is shown in the report (the file
    /// being visualized); it does not make the report live.
    /// </summary>
    public string RenderHtmlReport(string source, string? documentPath = null) =>
        HtmlTemplate.RenderPage(BuildStages(source), source, VisualizationMode.Static, documentPath);

    /// <summary>
    /// Compiles the source and renders the HTML report as a <b>live</b> report: the
    /// injected payload carries the given <paramref name="mode"/> (watch or live) and
    /// <paramref name="documentPath"/>, so the client opens a live session
    /// (subscribes for hot-reload pushes) instead of showing a static report.
    /// </summary>
    public string RenderLiveReport(string documentPath, string source, string mode)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        return HtmlTemplate.RenderPage(BuildStages(source), source, mode, documentPath);
    }

    /// <summary>
    /// Compiles the source and serializes the current document payload
    /// (<c>{ mode, path, source, stages }</c>) as JSON, for the live server's
    /// document API and its hot-reload push events.
    /// </summary>
    public string SerializeDocument(string documentPath, string source, string mode)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        return DisplayGraphJson.SerializeDocument(mode, documentPath, source, BuildStages(source));
    }

    private static void CollectLocalImages(
        object node, MarkdownAstProjection projection, List<string> references)
    {
        if (node is ImageInline image && IsLocalReference(image.Source))
        {
            references.Add(image.Source);
        }

        foreach (var child in projection.Neighbors(node))
        {
            CollectLocalImages(child, projection, references);
        }
    }

    private static bool IsLocalReference(string source) =>
        !source.StartsWith("//", StringComparison.Ordinal)
        && !(Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "data" or "ftp" or "mailto");
}
