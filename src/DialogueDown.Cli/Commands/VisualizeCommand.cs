using DialogueDown.Visualization.Live;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>visualize</c> command: render a script's compilation. Static mode writes a
/// self-contained report and opens it; <c>--watch</c> serves the report from a local
/// server and hot-reloads it. The work is delegated to the visualization engine
/// through <see cref="IVisualizeRunner"/>.
/// </summary>
internal sealed class VisualizeCommand : AsyncCommand<VisualizeSettings>
{
    private readonly IVisualizeRunner _runner;

    public VisualizeCommand(IVisualizeRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        _runner = runner;
    }

    /// <inheritdoc />
    protected override Task<int> ExecuteAsync(
        CommandContext context, VisualizeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Watch)
        {
            return _runner.RunWatchAsync(
                settings.Script, settings.Port, settings.NoOpen, settings.RenderRoot, cancellationToken);
        }

        return Task.FromResult(_runner.RunStatic(settings.Script, settings.Output, settings.NoOpen));
    }
}
