using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization.Configuration;
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
        Assert.Contains("\"mode\":\"view\"", html); // view is the default session mode
    }

    [Fact]
    public void Mode_Explicit_IsCarriedIntoThePayload()
    {
        using var doc = new TempDocument("# Scene");

        var session = new LiveSession(doc.Path, "edit");

        Assert.Equal("edit", session.Mode);
        Assert.Contains("\"mode\":\"edit\"", session.CurrentDocumentJson());
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
        var session = new LiveSession("/tmp/missing-edit.dialogue.md");
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
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save("# New\n\nAlice: Hi");

        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(doc.Path));
        Assert.Contains("\"source\":\"# New", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void Refresh_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save("# Saved");
        session.Refresh(); // the watcher firing for the browser's own write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Refresh_ExternalChangeAfterSave_StillBroadcasts()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save("# Saved");
        File.WriteAllText(doc.Path, "# External");
        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# External", received.Data);
    }

    [Fact]
    public void SaveConfig_WritesTheFileAndRecompilesWithTheNewSpeakers()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);

        var json = session.SaveConfig(Speaker("Bob", "B"));

        Assert.Contains("Bob", File.ReadAllText(configPath)); // written to disk
        Assert.Contains("\"name\":\"Bob\"", json); // recompiled payload carries the new speaker
        // The old configured speaker is gone (only Bob is configured now); the "Alice" in the
        // dialogue's own text is unrelated, so assert on the configured-speaker shape.
        Assert.DoesNotContain("\"name\":\"Alice\"", json);
    }

    [Fact]
    public void SaveConfig_WithoutAConfigFile_Throws()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path, VisualizationMode.Edit);

        Assert.Throws<InvalidOperationException>(() => session.SaveConfig(Speaker("Bob", "B")));
    }

    [Fact]
    public void CreateConfig_WritesTheStarterFileAndAdoptsIt()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit); // no configuration
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        var json = session.CreateConfig(configPath);

        Assert.True(File.Exists(configPath)); // the starter file is on disk
        Assert.Contains("[[speakers]]", File.ReadAllText(configPath)); // seeded with the template
        Assert.Contains("dialogue.toml", json); // the payload now carries the configuration file
        // Adopted: a later edit saves to the same file and recompiles with the new speaker.
        var saved = session.SaveConfig(Speaker("Bob", "B"));
        Assert.Contains("\"name\":\"Bob\"", saved);
        Assert.Contains("Bob", File.ReadAllText(configPath));
    }

    [Fact]
    public void CreateConfig_WhenTheSessionAlreadyHasAConfig_Throws()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);

        Assert.Throws<InvalidOperationException>(
            () => session.CreateConfig(Path.Combine(tree.Root, "other.toml")));
    }

    [Fact]
    public void SaveConfig_MalformedToml_WritesThenThrows()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        Assert.Throws<DialogueConfigurationException>(() => session.SaveConfig(broken));
        Assert.Equal(broken, File.ReadAllText(configPath)); // force-overwrite, like a broken document save
    }

    private static LiveSession ConfiguredSession(string docPath, string configPath)
    {
        var source = File.ReadAllText(configPath);
        var configuration = AppliedConfiguration.FromFile(
            configPath, source, TomlConfigurationLoader.Parse(source, configPath));
        return new LiveSession(
            docPath, VisualizationMode.Edit, new CompilationVisualizer(configuration), configPath);
    }

    private static string Speaker(string name, string id) =>
        $"[[speakers]]\nname = \"{name}\"\nid = \"{id}\"\n";
}
