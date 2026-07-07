namespace DialogueDown.Visualization;

/// <summary>
/// Assembles a self-contained HTML report from one or more display graphs.
/// The page itself — D3, marked, Pico.css, Tippy, the stylesheet, and the client
/// script — is built ahead of time by the <c>web/</c> Vite project into a single
/// file (<c>web/dist/report.html</c>) that is embedded in this assembly. All this
/// step does is inject the per-report stage data into that file's data slot, so
/// the report opens in any modern browser with no network and no files on disk.
/// Each graph becomes a tab. Used for a single graph (<see cref="HtmlRenderer"/>)
/// and for the multi-stage report.
/// </summary>
internal static class HtmlTemplate
{
    private const string StagesSlot = "\"__STAGES__\"";

    public static string RenderPage(IReadOnlyList<DisplayGraph> stages)
    {
        return EmbeddedAsset.ReadText("report.html")
            .Replace(StagesSlot, DisplayGraphJson.Serialize(stages));
    }
}
