namespace DialogueDown.Visualization;

/// <summary>
/// Assembles a single self-contained HTML page from one or more display graphs.
/// The D3 library, the stylesheet, and the client script are all inlined from
/// embedded assets, so the page opens in any browser with no network or server.
/// Each graph becomes a tab. Used for a single graph (<see cref="HtmlRenderer"/>)
/// and for the multi-stage report.
/// </summary>
internal static class HtmlTemplate
{
    public static string RenderPage(IReadOnlyList<DisplayGraph> stages)
    {
        return EmbeddedAsset.ReadText("report.html")
            .Replace("__CSS__", EmbeddedAsset.ReadText("report.css"))
            .Replace("__D3__", EmbeddedAsset.ReadText("d3.v7.min.js"))
            .Replace("__STAGES__", DisplayGraphJson.Serialize(stages))
            .Replace("__REPORT_JS__", EmbeddedAsset.ReadText("report.js"));
    }
}
