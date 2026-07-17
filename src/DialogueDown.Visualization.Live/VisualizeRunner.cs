using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// The default <see cref="IVisualizeRunner"/>: hides the server and consent wiring
/// behind the static export and the served session, opening results with the injected
/// browser launcher.
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
    public int RunStatic(string file, string? output, bool noOpen, AppliedConfiguration configuration) =>
        StaticMode.Run(file, output, noOpen, configuration, _browser, Console.Error);

    /// <inheritdoc />
    public int RunEmit(string file, EmitFormat format, string? output, CompilerOptions options) =>
        EmitMode.Run(file, format, output, options, Console.Out, Console.Error);

    /// <inheritdoc />
    public Task<int> RunServedAsync(
        string file, int? port, bool noOpen, string? renderRoot, string mode,
        AppliedConfiguration configuration, CancellationToken cancellationToken)
    {
        var consent = new ConsoleHostConsent(!Console.IsInputRedirected, Console.In, Console.Out);
        return ServeMode.RunAsync(
            file, port, noOpen, renderRoot, configuration, _browser, consent, Console.Out, Console.Error,
            cancellationToken, mode);
    }
}
