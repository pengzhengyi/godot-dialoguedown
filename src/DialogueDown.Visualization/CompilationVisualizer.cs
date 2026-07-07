using DialogueDown.Markdown;

namespace DialogueDown.Visualization;

/// <summary>
/// The entry facade for compilation visualization: it compiles a script string,
/// projects each available stage into a display graph, and assembles a single
/// self-contained, multi-tab HTML report.
/// </summary>
/// <remarks>
/// Only the Markdown AST stage exists on <c>main</c> today. The Dialogue AST stage
/// is appended in <see cref="BuildStages"/> once the transpiler lands, with no
/// change to the walk, model, or renderers.
/// </remarks>
internal sealed class CompilationVisualizer
{
    private readonly IMarkdownParser _parser;

    public CompilationVisualizer()
        : this(new MarkdigMarkdownParser())
    {
    }

    public CompilationVisualizer(IMarkdownParser parser)
    {
        ArgumentNullException.ThrowIfNull(parser);
        _parser = parser;
    }

    /// <summary>Compiles the source and projects each stage into a display graph.</summary>
    public IReadOnlyList<DisplayGraph> BuildStages(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var markdown = _parser.Parse(source);
        return [markdown.ToDisplayGraph()];
    }

    /// <summary>Compiles the source and renders the multi-tab HTML report.</summary>
    public string RenderHtmlReport(string source) =>
        HtmlTemplate.RenderPage(BuildStages(source));
}
