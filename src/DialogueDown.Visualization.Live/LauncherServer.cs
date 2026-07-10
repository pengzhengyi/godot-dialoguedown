using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A loopback launcher server. It serves the launcher page at <c>/</c>, browses the local
/// filesystem for <c>.dialogue.md</c> sources (<c>GET /api/browse</c> — unconfined, like a
/// native "Open Folder" dialog), and on <c>POST /api/open</c> opens the chosen source
/// under the chosen root: it swaps in a live session — static or watched — and serves its
/// report under <c>/r/</c>. Browsing is unconfined, but serving is confined to the opened
/// root (validated with <see cref="LaunchRoot"/>, backed by a <see cref="MutableFileRoot"/>
/// that follows it) and the server is loopback-only.
/// </summary>
internal sealed class LauncherServer : IAsyncDisposable
{
    private const string ReportMount = "/r";

    private readonly WebApplication _app;
    private readonly string _startRoot;
    private readonly string _launcherHtml;
    private readonly Func<string, string, LiveSession> _sessionFactory;
    private readonly MutableFileRoot _serveRoot = new();
    private readonly object _gate = new();
    private ActiveDocument? _active;

    /// <summary>
    /// Builds a launcher server that starts browsing at <paramref name="startRoot"/> on the
    /// given loopback port (0 = ephemeral), serving <paramref name="launcherHtml"/> at <c>/</c>.
    /// </summary>
    public LauncherServer(
        string startRoot,
        string launcherHtml,
        int port = 0,
        Func<string, string, LiveSession>? sessionFactory = null)
    {
        _startRoot = Path.GetFullPath(startRoot);
        _launcherHtml = launcherHtml;
        _sessionFactory = sessionFactory ?? ((path, mode) => new LiveSession(path, mode));
        _serveRoot.Set(_startRoot);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders();
        _app = builder.Build();
        Configure(_app);
    }

    /// <summary>The base URL the server is listening on (valid after <see cref="StartAsync"/>).</summary>
    public string BaseUrl =>
        _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

    /// <summary>Starts listening.</summary>
    public Task StartAsync() => _app.StartAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _active?.Watcher?.Dispose();
        }

        await _app.DisposeAsync();
    }

    private static bool TryParseMode(string? mode, out string parsed)
    {
        parsed = mode?.ToLowerInvariant() switch
        {
            null or "" or VisualizationMode.Static => VisualizationMode.Static,
            VisualizationMode.Watch => VisualizationMode.Watch,
            _ => string.Empty,
        };
        return parsed.Length != 0;
    }

    private void Configure(WebApplication app)
    {
        // Assets for the active source resolve under the active root at /r/... . Static
        // files runs before routing (explicit UseRouting below) so it serves an existing
        // asset even though the catch-all report route would also match its path.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = _serveRoot,
            RequestPath = ReportMount,
        });
        app.UseRouting();

        app.MapGet("/", () => Results.Content(_launcherHtml, "text/html; charset=utf-8"));
        app.MapGet("/api/browse", (string? path) => Browse(path));
        app.MapPost("/api/open", (OpenRequest request, HttpContext context) => Open(request, context));
        app.MapGet("/api/document", Document);
        app.MapGet("/api/events", HandleEventsAsync);
        app.MapGet(ReportMount, () => Report(string.Empty));
        app.MapGet(ReportMount + "/{**path}", (string? path) => Report(path ?? string.Empty));
    }

    private IResult Browse(string? path)
    {
        var listing = DirectoryBrowser.List(string.IsNullOrEmpty(path) ? _startRoot : path);
        return listing is null ? Results.NotFound() : Results.Json(listing.Value);
    }

    private IResult Open(OpenRequest request, HttpContext context)
    {
        if (string.IsNullOrEmpty(request.Root) || !Directory.Exists(request.Root))
        {
            return Results.NotFound();
        }

        var root = LaunchRoot.At(request.Root);
        var relative = Path.GetRelativePath(root.RootDirectory, Path.GetFullPath(request.Source ?? string.Empty));
        var source = root.ResolveSource(relative);
        if (source is null)
        {
            return Results.NotFound();
        }

        if (!TryParseMode(request.Mode, out var mode))
        {
            return Results.BadRequest(new { message = $"Unsupported mode: {request.Mode}" });
        }

        var reportPath = ServeRoot.For(root.RootDirectory, Path.GetDirectoryName(source)!).ReportPath;
        var session = _sessionFactory(source, mode);
        var watcher = mode == VisualizationMode.Watch ? new DocumentWatcher(source, session.Refresh) : null;

        lock (_gate)
        {
            _active?.Watcher?.Dispose();
            _serveRoot.Set(root.RootDirectory);
            _active = new ActiveDocument(session, reportPath.Trim('/'), watcher);
        }

        context.Response.Headers.Location = ReportMount + reportPath;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

    private IResult Report(string path)
    {
        var active = Active();
        if (active is null || path.Trim('/') != active.ReportRelative)
        {
            return Results.NotFound();
        }

        return Results.Content(active.Session.RenderInitialHtml(), "text/html; charset=utf-8");
    }

    private IResult Document()
    {
        var active = Active();
        return active is null
            ? Results.NotFound()
            : Results.Content(active.Session.CurrentDocumentJson(), "application/json; charset=utf-8");
    }

    private async Task HandleEventsAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var active = Active();
        if (active is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        using var subscription = active.Session.Broadcaster.Subscribe(out var reader);
        await context.Response.Body.FlushAsync(cancellationToken);

        await foreach (var liveEvent in reader.ReadAllAsync(cancellationToken))
        {
            await context.Response.WriteAsync($"event: {liveEvent.Event}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {liveEvent.Data}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    private ActiveDocument? Active()
    {
        lock (_gate)
        {
            return _active;
        }
    }

    private sealed record ActiveDocument(LiveSession Session, string ReportRelative, DocumentWatcher? Watcher);

    private sealed record OpenRequest(string? Root, string? Source, string? Mode);
}
