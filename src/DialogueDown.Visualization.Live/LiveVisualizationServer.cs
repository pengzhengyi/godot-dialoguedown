using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A loopback-only web server that serves a document's live report and streams
/// hot-reload events over Server-Sent Events. Read-only: it exposes no write
/// routes. Binds <c>127.0.0.1</c> on an ephemeral port unless one is given.
/// </summary>
internal sealed class LiveVisualizationServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly LiveSession _session;

    /// <summary>Builds a server for <paramref name="session"/> on the given loopback port (0 = ephemeral).</summary>
    public LiveVisualizationServer(LiveSession session, int port = 0)
    {
        _session = session;
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
    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private void Configure(WebApplication app)
    {
        // Serve files alongside the document (e.g. `assets/painting.jpg`) so the
        // report's relative image and resource links resolve. Rooted at the
        // document's own directory, so only its siblings are exposed (loopback,
        // dev-only); traversal outside the root is blocked by the middleware.
        var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(_session.DocumentPath));
        if (documentDirectory is not null)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(documentDirectory),
                RequestPath = string.Empty,
            });
        }

        app.MapGet(
            "/",
            () => Results.Content(_session.RenderInitialHtml(), "text/html; charset=utf-8"));
        app.MapGet(
            "/api/document",
            () => Results.Content(_session.CurrentDocumentJson(), "application/json; charset=utf-8"));
        app.MapGet("/api/events", HandleEventsAsync);
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
}
