using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// The interactive <c>visualize &lt;file&gt;</c> path: start a loopback server for the
/// document, watch the file, and recompile-and-push on every change, until the process
/// is interrupted (Ctrl+C). The same served session hosts both <b>View</b> (read-only,
/// auto-updating) and <b>Edit</b> (in-browser editing that saves back to the file);
/// <paramref name="mode"/> only chooses which side the browser opens on — the reader can
/// toggle at runtime. The offline snapshot is a separate export (see <c>StaticMode</c>).
/// </summary>
internal static class ServeMode
{
    /// <summary>
    /// Runs a served session until <paramref name="cancellationToken"/> is canceled.
    /// <paramref name="mode"/> is the initial side of the View/Edit toggle
    /// (<see cref="VisualizationMode.View"/> or <see cref="VisualizationMode.Edit"/>).
    /// Returns 0 on a clean stop, or 1 when the document is invalid or an explicit
    /// <paramref name="renderRoot"/> cannot be used.
    /// </summary>
    public static async Task<int> RunAsync(
        string file,
        int? port,
        bool noOpen,
        string? renderRoot,
        AppliedConfiguration configuration,
        IBrowserLauncher browser,
        IHostConsent consent,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken,
        string mode = VisualizationMode.View)
    {
        // Create-on-edit: an Edit session may target a not-yet-existing script. Create it
        // empty (when the name is a .dialogue.md in an existing folder) so the served path —
        // which reads the file — works unchanged. View has nothing to show for a missing
        // file, so it still errors below.
        if (mode == VisualizationMode.Edit)
        {
            CreateIfMissing(file);
        }

        var problem = DocumentValidation.Validate(file);
        if (problem is not null)
        {
            error.WriteLine(problem);
            return 1;
        }

        var fullPath = Path.GetFullPath(file);
        var references = new CompilationVisualizer().LocalImageReferences(File.ReadAllText(fullPath));
        var serveRoot = ServeRootResolver.Resolve(fullPath, references, renderRoot, consent, error);
        if (serveRoot is null)
        {
            return 1;
        }

        var session = new LiveSession(
            fullPath, mode, new CompilationVisualizer(configuration), configuration.File?.Path);
        await using var server = new LiveVisualizationServer(session, port ?? 0, serveRoot);
        await server.StartAsync();
        using var watcher = new DocumentWatcher(fullPath, session.Refresh);

        var url = server.ReportUrl;
        var verb = mode == VisualizationMode.Edit ? "editing" : "visualization";
        output.WriteLine($"Live {verb} of {fullPath}");
        output.WriteLine($"  {url}  (press Ctrl+C to stop)");
        if (!noOpen)
        {
            browser.Open(url);
        }

        // Keep serving until canceled (Ctrl+C). Complete normally on cancellation
        // rather than throwing, so shutdown is not an exceptional path.
        var stopped = new TaskCompletionSource();
        await using var registration = cancellationToken.Register(() => stopped.TrySetResult());
        await stopped.Task;

        return 0;
    }

    // Writes an empty script when `file` is a not-yet-existing `.dialogue.md` in an existing
    // folder, so an Edit session can start on a fresh file. A wrong extension or a missing
    // folder is left for DocumentValidation to report.
    private static void CreateIfMissing(string file)
    {
        if (File.Exists(file) ||
            !file.EndsWith(DocumentValidation.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(file));
        if (directory is not null && Directory.Exists(directory))
        {
            File.WriteAllText(file, string.Empty);
        }
    }
}
