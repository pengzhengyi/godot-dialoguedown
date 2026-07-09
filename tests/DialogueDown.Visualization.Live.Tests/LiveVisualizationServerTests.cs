using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveVisualizationServerTests
{
    [Fact]
    public async Task Root_ServesTheLiveReportHtml()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var html = await client.GetStringAsync("/");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"watch\"", html);
    }

    [Fact]
    public async Task ServesFilesAlongsideTheDocument_SoRelativeImagesResolve()
    {
        using var doc = new TempDocument("# Scene\n\n![pic](assets/pic.png)");
        var assetsDir = Path.Combine(Path.GetDirectoryName(doc.Path)!, "assets");
        Directory.CreateDirectory(assetsDir);
        var pic = Path.Combine(assetsDir, "pic.png");
        var bytes = new byte[] { 1, 2, 3, 4 };
        await File.WriteAllBytesAsync(pic, bytes);

        try
        {
            await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
            await server.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

            var response = await client.GetAsync("/assets/pic.png");

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            File.Delete(pic);
            Directory.Delete(assetsDir);
        }
    }

    [Fact]
    public async Task DocumentApi_ReturnsThePayloadJson()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var response = await client.GetAsync("/api/document");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"source\":\"# Scene\"", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public async Task Events_StreamsAReloadWhenTheSessionRefreshes()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = timeout.Token;
        using var doc = new TempDocument("# First");
        var session = new LiveSession(doc.Path);
        await using var server = new LiveVisualizationServer(session);
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        using var response = await client.GetAsync(
            "/api/events",
            HttpCompletionOption.ResponseHeadersRead,
            token);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);

        File.WriteAllText(doc.Path, "# Second");
        session.Refresh();

        var sawReload = false;
        var sawContent = false;
        string? line;
        while ((line = await reader.ReadLineAsync(token)) is not null)
        {
            if (line.Contains("event: reload", StringComparison.Ordinal))
            {
                sawReload = true;
            }

            if (line.Contains("# Second", StringComparison.Ordinal))
            {
                sawContent = true;
                break;
            }
        }

        Assert.True(sawReload);
        Assert.True(sawContent);
    }
}
