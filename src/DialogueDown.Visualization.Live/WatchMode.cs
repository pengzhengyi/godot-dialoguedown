namespace DialogueDown.Visualization.Live;

/// <summary>
/// The <c>visualize &lt;file&gt; --watch</c> and <c>--live</c> path: start a loopback
/// live server for the document, watch the file, and recompile-and-push on every change,
/// until the process is interrupted (Ctrl+C). Watch is read-only — the file changes from
/// your editor, not the page; Live Edit serves an editable report that saves back to the
/// file.
/// </summary>
internal static class WatchMode
{
    /// <summary>
    /// Runs a watch or live session until <paramref name="cancellationToken"/> is
    /// canceled. <paramref name="mode"/> selects <c>watch</c> (read-only) or <c>live</c>
    /// (editable). Returns 0 on a clean stop, or 1 when the document is invalid or an
    /// explicit <paramref name="renderRoot"/> cannot be used.
    /// </summary>
    public static async Task<int> RunAsync(
        string file,
        int? port,
        bool noOpen,
        string? renderRoot,
        IBrowserLauncher browser,
        IHostConsent consent,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken,
        string mode = VisualizationMode.Watch)
    {
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

        var session = new LiveSession(fullPath, mode);
        await using var server = new LiveVisualizationServer(session, port ?? 0, serveRoot);
        await server.StartAsync();
        using var watcher = new DocumentWatcher(fullPath, session.Refresh);

        var url = server.ReportUrl;
        var verb = mode == VisualizationMode.Live ? "editing" : "visualization";
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
}
