using DialogueDown.Markdown;

namespace DialogueDown.Visualization;

/// <summary>
/// The entry facade for compilation visualization: it compiles a script string,
/// projects each available stage into a display graph, and assembles a single
/// self-contained, multi-tab HTML report. The live-visualization server consumes
/// the same facade to render and serialize a document.
/// </summary>
/// <remarks>
/// Only the Markdown AST stage exists on <c>main</c> today. The Dialogue AST stage
/// is appended in <see cref="BuildStages"/> once the transpiler lands, with no
/// change to the walk, model, or renderers.
/// </remarks>
public sealed class CompilationVisualizer
{
    private readonly IMarkdownParser _parser;

    /// <summary>Creates a visualizer using the default Markdig-based parser.</summary>
    public CompilationVisualizer()
        : this(new MarkdigMarkdownParser())
    {
    }

    /// <summary>Creates a visualizer over a specific <paramref name="parser"/>.</summary>
    internal CompilationVisualizer(IMarkdownParser parser)
    {
        ArgumentNullException.ThrowIfNull(parser);
        _parser = parser;
    }

    /// <summary>Compiles the source and projects each stage into a display graph.</summary>
    public IReadOnlyList<DisplayGraph> BuildStages(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var markdown = _parser.Parse(source);
        return [markdown.ToDisplayGraph(source)];
    }

    /// <summary>Compiles the source and renders the static, multi-tab HTML report.</summary>
    public string RenderHtmlReport(string source) =>
        HtmlTemplate.RenderPage(BuildStages(source), source);

    /// <summary>
    /// Compiles the source and renders the HTML report as a <b>live</b> report: the
    /// injected payload carries a live marker with <paramref name="documentPath"/>,
    /// so the client opens a live session (subscribes for hot-reload pushes) instead
    /// of showing a static report.
    /// </summary>
    public string RenderLiveReport(string documentPath, string source)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        return HtmlTemplate.RenderPage(BuildStages(source), source, documentPath);
    }

    /// <summary>
    /// Compiles the source and serializes the current document payload
    /// (<c>{ path, source, stages }</c>) as JSON, for the live server's document
    /// API and its hot-reload push events.
    /// </summary>
    public string SerializeDocument(string documentPath, string source)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        return DisplayGraphJson.SerializeDocument(documentPath, source, BuildStages(source));
    }
}
