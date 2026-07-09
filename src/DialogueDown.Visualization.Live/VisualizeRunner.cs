namespace DialogueDown.Visualization.Live;

/// <summary>
/// The public entry to the visualization run modes, driven by the
/// <c>dialoguedown visualize</c> command. It hides the server and consent wiring
/// behind two calls: a one-shot static render, and a watch session that serves the
/// live report until cancelled.
/// </summary>
public static class VisualizeRunner
{
    /// <summary>
    /// Renders <paramref name="file"/> to a self-contained report and opens it via
    /// <paramref name="browser"/> (unless <paramref name="noOpen"/>), or writes it to
    /// <paramref name="output"/>. Returns a process exit code.
    /// </summary>
    public static int RunStatic(
        string file, string? output, bool noOpen, IBrowserLauncher browser, TextWriter error) =>
        StaticMode.Run(file, output, noOpen, browser, error);

    /// <summary>
    /// Serves the live report for <paramref name="file"/> on a loopback port and
    /// hot-reloads it until <paramref name="cancellationToken"/> is cancelled.
    /// <paramref name="renderRoot"/> pins the static-asset root (otherwise it is
    /// resolved, with consent, from the document's referenced images). Returns a
    /// process exit code.
    /// </summary>
    public static Task<int> RunWatchAsync(
        string file,
        int? port,
        bool noOpen,
        string? renderRoot,
        IBrowserLauncher browser,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var consent = new ConsoleHostConsent(!Console.IsInputRedirected, Console.In, Console.Out);
        return WatchMode.RunAsync(
            file, port, noOpen, renderRoot, browser, consent, output, error, cancellationToken);
    }
}
