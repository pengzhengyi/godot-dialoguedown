using DialogueDown.ConfigurationLoader;
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

    // Saves the posted buffer to the target file (the document, or its `dialogue.toml`
    // when target is "config") and returns the recompiled payload; a write or config-parse
    // failure surfaces as a 400 with a message rather than a 500.
    private IResult Save(SaveRequest request)
    {
        try
        {
            var source = request.Source ?? string.Empty;
            var isConfig = string.Equals(request.Target, "config", StringComparison.OrdinalIgnoreCase);
            var json = isConfig ? _session.SaveConfig(source) : _session.Save(source);
            return Results.Content(json, "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or DialogueConfigurationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private sealed record SaveRequest(string? Source, string? Target = null);
}
