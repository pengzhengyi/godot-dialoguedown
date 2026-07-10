using System.Net;
using System.Net.Http.Json;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LauncherServerTests
{
    private const string LauncherHtml = "<!doctype html><title>Launcher</title>";

    [Fact]
    public async Task Root_ServesTheLauncherPage()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        var html = await client.GetStringAsync("/");

        Assert.Equal(LauncherHtml, html);
    }

    [Fact]
    public async Task Browse_ListsSubdirectoriesAndDialogueSources()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md", "# A");
        tree.File("root/notes.md");
        tree.Dir("root/proj");
        await using var server = await Started(tree);
        using var client = Client(server);

        var json = await client.GetStringAsync("/api/browse?path=");

        Assert.Contains("\"directories\":[\"proj\"]", json);
        Assert.Contains("\"sources\":[\"a.dialogue.md\"]", json);
        Assert.DoesNotContain("notes.md", json);
    }

    [Fact]
    public async Task Browse_OutsideRoot_NotFound()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.GetAsync("/api/browse?path=../");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Open_ValidSource_RedirectsToReportAndServesIt()
    {
        using var tree = new TempTree();
        tree.File("root/proj/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await client.PostAsJsonAsync(
            "/api/open", new { source = "proj/scene.dialogue.md", mode = "static" });

        Assert.Equal(HttpStatusCode.SeeOther, open.StatusCode);
        Assert.Equal("/r/proj/", open.Headers.Location!.ToString());

        var html = await client.GetStringAsync("/r/proj/");
        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"static\"", html);
    }

    [Fact]
    public async Task Open_WatchMode_ServesReportInWatchMode()
    {
        using var tree = new TempTree();
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await client.PostAsJsonAsync(
            "/api/open", new { source = "scene.dialogue.md", mode = "watch" });

        Assert.Equal("/r/", open.Headers.Location!.ToString());
        Assert.Contains("\"mode\":\"watch\"", await client.GetStringAsync("/r/"));
    }

    [Fact]
    public async Task Save_AfterOpeningLive_WritesTheDocumentAndReturnsStages()
    {
        using var tree = new TempTree();
        var path = tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);
        await client.PostAsJsonAsync("/api/open", new { source = "scene.dialogue.md", mode = "live" });

        var save = await client.PostAsJsonAsync("/api/save", new { source = "# Edited live\n" });

        Assert.True(save.IsSuccessStatusCode);
        var json = await save.Content.ReadAsStringAsync();
        Assert.Contains("\"stages\":", json);
        Assert.Equal("# Edited live\n", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Save_WhenActiveReportIsReadOnly_NotFound()
    {
        using var tree = new TempTree();
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);
        await client.PostAsJsonAsync("/api/open", new { source = "scene.dialogue.md", mode = "static" });

        var save = await client.PostAsJsonAsync("/api/save", new { source = "# Nope\n" });

        Assert.Equal(HttpStatusCode.NotFound, save.StatusCode);
    }

    [Fact]
    public async Task Open_NonDialogueSource_NotFound()
    {
        using var tree = new TempTree();
        tree.File("root/notes.md");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await client.PostAsJsonAsync("/api/open", new { source = "notes.md", mode = "static" });

        Assert.Equal(HttpStatusCode.NotFound, open.StatusCode);
    }

    [Fact]
    public async Task Report_BeforeOpen_NotFound()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/r/")).StatusCode);
    }

    [Fact]
    public async Task OpenedSource_ServesRelativeAssetsUnderReportMount()
    {
        using var tree = new TempTree();
        tree.File("root/proj/scene.dialogue.md", "# Scene\n\n![pic](art/pic.png)");
        await File.WriteAllBytesAsync(tree.File("root/proj/art/pic.png"), [1, 2, 3, 4]);
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        await client.PostAsJsonAsync("/api/open", new { source = "proj/scene.dialogue.md", mode = "static" });
        var asset = await client.GetAsync("/r/proj/art/pic.png");

        Assert.True(asset.IsSuccessStatusCode);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await asset.Content.ReadAsByteArrayAsync());
    }

    private static async Task<LauncherServer> Started(TempTree tree)
    {
        var server = new LauncherServer(LaunchRoot.At(tree.Dir("root")), LauncherHtml);
        await server.StartAsync();
        return server;
    }

    private static HttpClient Client(LauncherServer server, bool followRedirects = true)
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = followRedirects };
        return new HttpClient(handler) { BaseAddress = new Uri(server.BaseUrl) };
    }
}
