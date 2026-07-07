namespace DialogueDown.Visualization;

/// <summary>
/// Assembles a single self-contained HTML page from one or more display graphs.
/// D3, marked, Pico.css, the stylesheet, and the client script are all inlined
/// from embedded assets as offline fallbacks (each library is also requested from
/// a CDN first), so the page opens in any browser with or without a network. Each
/// graph becomes a tab. Used for a single graph (<see cref="HtmlRenderer"/>) and
/// for the multi-stage report.
/// </summary>
internal static class HtmlTemplate
{
    public static string RenderPage(IReadOnlyList<DisplayGraph> stages)
    {
        return EmbeddedAsset.ReadText("report.html")
            .Replace("__PICO_CSS__", EmbeddedAsset.ReadText("pico.min.css"))
            .Replace("__TIPPY_CSS__", EmbeddedAsset.ReadText("tippy.css"))
            .Replace("__CSS__", EmbeddedAsset.ReadText("report.css"))
            .Replace("__D3__", EmbeddedAsset.ReadText("d3.v7.min.js"))
            .Replace("__MARKED__", EmbeddedAsset.ReadText("marked.min.js"))
            .Replace("__POPPER__", EmbeddedAsset.ReadText("popper.min.js"))
            .Replace("__TIPPY_JS__", EmbeddedAsset.ReadText("tippy.umd.min.js"))
            .Replace("__STAGES__", DisplayGraphJson.Serialize(stages))
            .Replace("__REPORT_JS__", EmbeddedAsset.ReadText("report.js"));
    }
}
