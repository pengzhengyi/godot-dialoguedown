using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A loopback launcher server. It serves the launcher page at <c>/</c>, browses the
/// launch root for <c>.dialogue.md</c> sources, and on open swaps in a live session for
/// the chosen source — served once (static) or watched — under <c>/r/</c>. Browsing and
/// serving stay confined to the launch root (see <see cref="LaunchRoot"/>); the report is
/// mounted under <c>/r/</c> so it never collides with the launcher at <c>/</c>.
/// </summary>
internal sealed class LauncherServer : IAsyncDisposable
{
    private const string ReportMount = "/r";

    private readonly WebApplication _app;
    private readonly LaunchRoot _root;
    private readonly string _launcherHtml;
    private readonly Func<string, string, LiveSession> _sessionFactory;
    private readonly object _gate = new();
    private ActiveDocument? _active;

    /// <summary>
    /// Builds a launcher server for <paramref name="root"/> on the given loopback port
    /// (0 = ephemeral), serving <paramref name="launcherHtml"/> at <c>/</c>.
    /// </summary>
    public LauncherServer(
        LaunchRoot root,
        string launcherHtml,
        int port = 0,
        Func<string, string, LiveSession>? sessionFactory = null)
    {
        _root = root;
        _launcherHtml = launcherHtml;
        _sessionFactory = sessionFactory ?? ((path, mode) => new LiveSession(path, mode));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders();
        builder.AddLoopbackCompression();
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
            null or "" or VisualizationMode.View => VisualizationMode.View,
            VisualizationMode.Edit => VisualizationMode.Edit,
            _ => string.Empty,
        };
        return parsed.Length != 0;
    }

    private void Configure(WebApplication app)
    {
        // Compress the large report pages; text/event-stream is not compressible, so the
        // SSE hot-reload stream passes through untouched.
        app.UseResponseCompression();

        // Assets for the active source resolve under the launch root at /r/... . Static
        // files runs before routing (explicit UseRouting below) so it serves an existing
        // asset even though the catch-all report route would also match its path.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(_root.RootDirectory),
            RequestPath = ReportMount,
        });
        app.UseRouting();

        app.MapGet("/", () => Results.Content(_launcherHtml, "text/html; charset=utf-8"));
        app.MapGet("/api/browse", (string? path) => Browse(path ?? string.Empty));
        app.MapPost("/api/open", (OpenRequest request, HttpContext context) => Open(request, context));
        app.MapGet("/api/document", Document);
        app.MapPost("/api/save", (SaveRequest request) => Save(request));
        app.MapGet("/api/events", HandleEventsAsync);
        app.MapGet(ReportMount, () => Report(string.Empty));
        app.MapGet(ReportMount + "/{**path}", (string? path) => Report(path ?? string.Empty));
    }

    private IResult Browse(string path)
    {
        var listing = _root.Browse(path);
        return listing is null ? Results.NotFound() : Results.Json(listing.Value);
    }

    private IResult Open(OpenRequest request, HttpContext context)
    {
        var source = _root.ResolveSource(request.Source ?? string.Empty);
        if (source is null)
        {
            return Results.NotFound();
        }

        if (!TryParseMode(request.Mode, out var mode))
        {
            return Results.BadRequest(new { message = $"Unsupported mode: {request.Mode}" });
        }

        var sourceDirectory = Path.GetDirectoryName(source)!;
        var reportPath = ServeRoot.For(_root.RootDirectory, sourceDirectory).ReportPath;
        var session = _sessionFactory(source, mode);
        // A served session always watches the file: View hot-reloads the report, Edit
        // surfaces a passive "changed on disk" chip.
        var watcher = new DocumentWatcher(source, session.Refresh);

        lock (_gate)
        {
            _active?.Watcher?.Dispose();
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

    // Saves the edited buffer to the active document. A served session always accepts
    // this (the client only calls it in Edit); there is just nothing active before a
    // script is opened.
    private IResult Save(SaveRequest request)
    {
        var active = Active();
        if (active is null)
        {
            return Results.NotFound();
        }

        try
        {
            var json = active.Session.Save(request.Source ?? string.Empty);
            return Results.Content(json, "application/json; charset=utf-8");
        }
        catch (IOException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
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

    private sealed record OpenRequest(string? Source, string? Mode);

    private sealed record SaveRequest(string? Source);
}
