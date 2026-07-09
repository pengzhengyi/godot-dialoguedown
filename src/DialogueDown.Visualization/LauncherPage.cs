using System.Text.Json;

namespace DialogueDown.Visualization;

/// <summary>
/// The launcher landing page: the embedded launcher client
/// (<c>web/dist/launcher.html</c>) with its initial selection injected into the page's
/// data slot, so the launcher opens pre-filled with the root, an optional pre-selected
/// source, and a mode.
/// </summary>
public static class LauncherPage
{
    private const string SelectionSlot = "\"__LAUNCHER__\"";

    /// <summary>
    /// Renders the launcher page for the given display <paramref name="root"/>, an
    /// optional root-relative <paramref name="source"/> to pre-select, and a
    /// <paramref name="mode"/>.
    /// </summary>
    public static string Render(string root, string? source, string mode) =>
        EmbeddedAsset.ReadText("launcher.html")
            .Replace(SelectionSlot, JsonSerializer.Serialize(new { root, source, mode }));
}
