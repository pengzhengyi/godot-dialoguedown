namespace DialogueDown.Visualization.Live;

/// <summary>
/// The default <see cref="IVisualizeRunner"/>: hides the server and consent wiring
/// behind the two run modes, opening results with the injected browser launcher.
/// </summary>
public sealed class VisualizeRunner : IVisualizeRunner
{
    private readonly IBrowserLauncher _browser;

    public VisualizeRunner(IBrowserLauncher browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <inheritdoc />
    public int RunStatic(string file, string? output, bool noOpen) =>
        StaticMode.Run(file, output, noOpen, _browser, Console.Error);

    /// <inheritdoc />
    public Task<int> RunWatchAsync(
        string file, int? port, bool noOpen, string? renderRoot, CancellationToken cancellationToken)
    {
        var consent = new ConsoleHostConsent(!Console.IsInputRedirected, Console.In, Console.Out);
        return WatchMode.RunAsync(
            file, port, noOpen, renderRoot, _browser, consent, Console.Out, Console.Error, cancellationToken);
    }
}
