using DialogueDown.Configuration;
using DialogueDown.Visualization;
using DialogueDown.Visualization.Live;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>visualize</c> command. Given a script it opens a <b>served session</b>
/// directly — read-only <b>View</b> by default, or editable <b>Edit</b> with
/// <c>--edit</c> — where the reader toggles View/Edit in the browser. With no script (or
/// <c>--pick</c>) it opens the launcher to browse for one. <c>-o</c> is a non-interactive
/// static export. Every report is compiled with the project's resolved
/// <see cref="CompilerOptions"/>. The work is delegated to the visualization engine through
/// <see cref="IVisualizeRunner"/> (direct) and <see cref="ILauncherRunner"/> (launcher).
/// </summary>
internal sealed class VisualizeCommand : AsyncCommand<VisualizeSettings>
{
    private readonly IVisualizeRunner _runner;
    private readonly ILauncherRunner _launcher;
    private readonly ProjectConfiguration _configuration;

    public VisualizeCommand(
        IVisualizeRunner runner, ILauncherRunner launcher, ProjectConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(configuration);
        _runner = runner;
        _launcher = launcher;
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Task<int> ExecuteAsync(
        CommandContext context, VisualizeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var hasScript = !string.IsNullOrWhiteSpace(settings.Script);

        // A non-interactive emit writes stage text (Mermaid/DOT) to --output or stdout,
        // never a server. Checked before the HTML export so `--emit dot -o x.dot` emits
        // text rather than an HTML report. The format is validated in settings, so
        // parsing here always succeeds.
        if (settings.Emit is not null
            && VisualizeSettings.TryParseEmitFormat(settings.Emit, out var format))
        {
            return Task.FromResult(
                _runner.RunEmit(settings.Script, format, settings.Output, OptionsForScript(settings)));
        }

        // A non-interactive HTML export never opens a server or the launcher.
        if (settings.Output is not null)
        {
            return Task.FromResult(
                _runner.RunStatic(settings.Script, settings.Output, settings.NoOpen, OptionsForScript(settings)));
        }

        var mode = settings.Edit ? LaunchMode.Edit : LaunchMode.View;

        // A script opens a served session directly (View, or Edit with --edit); the
        // launcher is for browsing (no script) or when forced with --pick.
        if (hasScript && !settings.Pick)
        {
            return _runner.RunServedAsync(
                settings.Script, settings.Port, settings.NoOpen, settings.Root,
                settings.Edit ? VisualizationMode.Edit : VisualizationMode.View,
                OptionsForScript(settings), cancellationToken);
        }

        var (root, source) = ResolveLaunch(settings.Script, hasScript, settings.Root);
        var options = _configuration.Resolve(settings.Config, root, root);
        return _launcher.RunAsync(root, source, mode, settings.Port, settings.NoOpen, options, cancellationToken);
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

    // Discover the project's options from the script's folder upward, never above --root.
    private CompilerOptions OptionsForScript(VisualizeSettings settings) =>
        _configuration.Resolve(
            settings.Config, Path.GetDirectoryName(Path.GetFullPath(settings.Script))!, settings.Root);
}
