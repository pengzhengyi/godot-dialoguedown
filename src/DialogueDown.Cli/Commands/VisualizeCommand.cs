using DialogueDown.Visualization.Live;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>visualize</c> command. The launcher is the uniform interactive entry point:
/// choose a script, a mode, and a root in the browser. Arguments pre-fill the launcher,
/// and a fully specified command (script, <c>--mode</c>, and <c>--root</c>, without
/// <c>--pick</c>) bypasses it to open the report directly. <c>-o</c> is a non-interactive
/// export. The work is delegated to the visualization engine through
/// <see cref="IVisualizeRunner"/> (direct) and <see cref="ILauncherRunner"/> (launcher).
/// </summary>
internal sealed class VisualizeCommand : AsyncCommand<VisualizeSettings>
{
    private readonly IVisualizeRunner _runner;
    private readonly ILauncherRunner _launcher;

    public VisualizeCommand(IVisualizeRunner runner, ILauncherRunner launcher)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(launcher);
        _runner = runner;
        _launcher = launcher;
    }

    /// <inheritdoc />
    protected override Task<int> ExecuteAsync(
        CommandContext context, VisualizeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var hasScript = !string.IsNullOrWhiteSpace(settings.Script);

        // A non-interactive export never opens the launcher.
        if (settings.Output is not null)
        {
            return Task.FromResult(_runner.RunStatic(settings.Script, settings.Output, settings.NoOpen));
        }

        var explicitMode = settings.Mode
            ?? (settings.Watch ? LaunchMode.Watch : settings.Live ? LaunchMode.Live : null);
        var mode = explicitMode ?? LaunchMode.Static;

        // Bypass to the report only when the source, mode, and root are all explicit.
        if (hasScript && explicitMode is not null && settings.Root is not null && !settings.Pick
            && mode != LaunchMode.Live)
        {
            return mode == LaunchMode.Watch
                ? _runner.RunWatchAsync(settings.Script, settings.Port, settings.NoOpen, settings.Root, cancellationToken)
                : Task.FromResult(_runner.RunStatic(settings.Script, null, settings.NoOpen));
        }

        var (root, source) = ResolveLaunch(settings.Script, hasScript, settings.Root);
        return _launcher.RunAsync(root, source, mode, settings.Port, settings.NoOpen, cancellationToken);
    }

    private static (string Root, string? Source) ResolveLaunch(string script, bool hasScript, string? rootOption)
    {
        var current = Directory.GetCurrentDirectory();
        if (!hasScript)
        {
            return (rootOption ?? current, null);
        }

        var fullScript = Path.GetFullPath(script);
        var root = rootOption
            ?? (IsUnder(current, fullScript) ? current : Path.GetDirectoryName(fullScript)!);
        var source = IsUnder(root, fullScript) ? Relative(root, fullScript) : null;
        return (root, source);
    }

    private static bool IsUnder(string root, string fullPath)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), fullPath);
        return relative != ".."
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    private static string Relative(string root, string fullPath) =>
        Path.GetRelativePath(Path.GetFullPath(root), fullPath).Replace(Path.DirectorySeparatorChar, '/');
}
