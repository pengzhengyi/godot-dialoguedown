using DialogueDown.Visualization.Configuration;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A loopback-only web server that serves a document's live report and streams
/// hot-reload events over Server-Sent Events. In Live Edit mode it also exposes a
/// single write route (<c>POST /api/save</c>) that writes the edited buffer back to
/// the document. Binds <c>127.0.0.1</c> on an ephemeral port unless one is given.
/// </summary>
internal sealed class LiveVisualizationServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly LiveSession _session;
    private readonly ServeRoot? _serveRoot;

    /// <summary>
    /// Builds a server for <paramref name="session"/> on the given loopback port
    /// (0 = ephemeral). <paramref name="serveRoot"/> is the folder to host and the
    /// URL path the report is served at; when omitted the document's own folder is
    /// hosted and the report sits at <c>/</c>.
    /// </summary>
    public LiveVisualizationServer(LiveSession session, int port = 0, ServeRoot? serveRoot = null)
    {
        _session = session;
        _serveRoot = serveRoot ?? DefaultServeRootFor(session.DocumentPath);
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

    /// <summary>The URL of the report itself — <see cref="BaseUrl"/> plus the report path.</summary>
    public string ReportUrl => BaseUrl.TrimEnd('/') + (_serveRoot?.ReportPath ?? "/");

    /// <summary>Starts listening.</summary>
    public Task StartAsync() => _app.StartAsync();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private static ServeRoot? DefaultServeRootFor(string documentPath)
    {
        var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(documentPath));
        return documentDirectory is null ? null : ServeRoot.For(documentDirectory, documentDirectory);
    }

    private void Configure(WebApplication app)
    {
        // Compress the large report page before anything else runs; the SSE stream
        // (text/event-stream) is not a compressible type, so it passes through.
        app.UseResponseCompression();

        // Serve the resolved root folder as static files so the report's relative
        // image and resource links resolve. Hosting is minimal and consent-gated:
        // the document's own folder by default, a broader ancestor only when the
        // caller resolved one (see ServeRootResolver). Traversal outside the root is
        // blocked by the middleware.
        var reportPath = _serveRoot?.ReportPath ?? "/";
        if (_serveRoot is { } serveRoot)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(serveRoot.RootDirectory),
                RequestPath = string.Empty,
            });
        }

        // The report sits at its sub-path under the root; a broadened root serves it
        // below `/`, so send `/` there for convenience.
        app.MapGet(
            reportPath,
            () => Results.Content(_session.RenderInitialHtml(), "text/html; charset=utf-8"));
        if (reportPath != "/")
        {
            app.MapGet("/", () => Results.Redirect(reportPath));
        }

        app.MapGet(
            "/api/document",
            () => Results.Content(_session.CurrentDocumentJson(), "application/json; charset=utf-8"));
        app.MapGet("/api/events", HandleEventsAsync);

        // A served session always accepts the write route; the client only calls it in
        // Edit. (The offline export has no server, so it can never reach this.)
        app.MapPost("/api/save", (SaveRequest request) => Save(request));

        // Reload the document or its configuration from disk (a conflict/uncertain recovery).
        app.MapPost("/api/reload", (ReloadRequest request) => Reload(request));

        // A session with no dialogue.toml can create one at the serve root; the client only
        // calls this in Edit, from the Config tab's no-config state.
        app.MapPost("/api/create-config", CreateConfig);
    }

    private async Task HandleEventsAsync(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        // Register before flushing headers so no event broadcast between the client
        // receiving headers and the subscription being live can slip through.
        using var subscription = _session.Broadcaster.Subscribe(out var reader);
        await context.Response.Body.FlushAsync(cancellationToken);

        // `cancellationToken` is the request's RequestAborted token. When the client
        // disconnects it cancels ReadAllAsync; ASP.NET treats that as a normal
        // disconnect, and the `using` above cleans up the subscription.
        await foreach (var liveEvent in reader.ReadAllAsync(cancellationToken))
        {
            await context.Response.WriteAsync($"event: {liveEvent.Event}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {liveEvent.Data}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    // Applies the posted save request (dialogue or config) and returns the typed-outcome
    // payload; a write failure or a missing config to save surfaces as a 400 with a message
    // rather than a 500.
    private IResult Save(SaveRequest request)
    {
        try
        {
            var json = _session.Save(
                new SaveInput(
                    request.Source,
                    request.Target,
                    request.ExpectedBaseline,
                    request.Validation,
                    request.Conflict));
            return Results.Content(json, "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // Reloads the document or its configuration from disk, returning the typed-outcome payload.
    private IResult Reload(ReloadRequest request)
    {
        try
        {
            return Results.Content(_session.Reload(request.Target), "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // Creates a dialogue.toml at the serve root for a session that has none, then returns the
    // recompiled payload (now carrying the configuration). The path is composed server-side
    // from the known serve root — never from the request — so no request value reaches the
    // filesystem. An existing file is a conflict (409), left untouched; a write failure is 400.
    private IResult CreateConfig()
    {
        var root = _serveRoot?.RootDirectory
            ?? Path.GetDirectoryName(Path.GetFullPath(_session.DocumentPath));
        if (root is null)
        {
            return Results.BadRequest(new { message = "The serve root could not be determined." });
        }

        var configPath = Path.Combine(root, ConfigurationFile.DefaultName);
        try
        {
            // A create is valid only when the file is absent. If it already exists but equals the
            // starter template, a retry after a lost response adopts it idempotently; any other
            // existing content is a conflict, left untouched.
            if (File.Exists(configPath))
            {
                if (File.ReadAllText(configPath) != ConfigStarter.Template)
                {
                    return Results.Conflict(
                        new { message = "A dialogue.toml already exists — reload to edit it." });
                }

                return Results.Content(
                    _session.AdoptExistingConfig(configPath), "application/json; charset=utf-8");
            }

            var json = _session.CreateConfig(configPath);
            return Results.Content(json, "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private sealed record SaveRequest(
        string? Source,
        string? Target = null,
        string? ExpectedBaseline = null,
        string? Validation = null,
        string? Conflict = null);

    private sealed record ReloadRequest(string? Target = null);
}
