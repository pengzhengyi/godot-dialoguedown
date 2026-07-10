using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveSessionTests
{
    [Fact]
    public void RenderInitialHtml_MarksThePayloadWithTheSessionMode()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path);

        var html = session.RenderInitialHtml();

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"watch\"", html); // watch is the default session mode
    }

    [Fact]
    public void Mode_DefaultsToWatch_AndIsCarriedIntoThePayload()
    {
        using var doc = new TempDocument("# Scene");

        var session = new LiveSession(doc.Path, "live");

        Assert.Equal("live", session.Mode);
        Assert.Contains("\"mode\":\"live\"", session.CurrentDocumentJson());
    }

    [Fact]
    public void CurrentDocumentJson_CarriesPathSourceAndStages()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path);

        var json = session.CurrentDocumentJson();

        Assert.Contains($"\"path\":", json);
        Assert.Contains("\"source\":\"# Scene\"", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void Refresh_BroadcastsAReloadWithTheCurrentContent()
    {
        using var doc = new TempDocument("# First");
        var session = new LiveSession(doc.Path);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(doc.Path, "# Second");

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# Second", received.Data);
    }

    [Fact]
    public void Refresh_MissingDocument_BroadcastsAProblem()
    {
        var session = new LiveSession("/tmp/missing-live.dialogue.md");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("not found", received.Data, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Save_WritesTheBufferToDiskAndReturnsCompiledStages()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "live");

        var json = session.Save("# New\n\nAlice: Hi");

        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(doc.Path));
        Assert.Contains("\"source\":\"# New", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void Refresh_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "live");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save("# Saved");
        session.Refresh(); // the watcher firing for the browser's own write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Refresh_ExternalChangeAfterSave_StillBroadcasts()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "live");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save("# Saved");
        File.WriteAllText(doc.Path, "# External");
        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# External", received.Data);
    }
}
