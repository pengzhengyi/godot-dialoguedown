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

        Assert.Contains("\"path\":", json);
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
        var session = new LiveSession(Path.Combine(Path.GetTempPath(), "missing-edit.dialogue.md"));
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("not found", received.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"target\":\"document\"", received.Data); // routes through the document controller
    }

    [Fact]
    public void Save_MatchingBaseline_WritesTheBufferAndReturnsSaved()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(new SaveInput("# New\n\nAlice: Hi", ExpectedBaseline: "# Old"));

        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(doc.Path));
        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Contains("\"source\":\"# New", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void Save_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var doc = new TempDocument("# External");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(new SaveInput("# Mine", ExpectedBaseline: "# Old"));

        Assert.Contains("\"outcome\":\"conflict\"", json);
        Assert.Equal("# External", File.ReadAllText(doc.Path)); // untouched
    }

    [Fact]
    public void Save_ConfirmedOverwrite_BypassesTheBaselineCheck()
    {
        using var doc = new TempDocument("# External");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(
            new SaveInput("# Mine", ExpectedBaseline: "# Old", Conflict: "overwrite"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Equal("# Mine", File.ReadAllText(doc.Path));
    }

    [Fact]
    public void Save_DiskAlreadyEqualsTheRequest_ReturnsIdempotentSaved()
    {
        using var doc = new TempDocument("# Same");
        var session = new LiveSession(doc.Path, "edit");

        // The disk already equals the requested source (a lost response), so a retry with a
        // different expected baseline still succeeds without a conflict.
        var json = session.Save(new SaveInput("# Same", ExpectedBaseline: "# Stale"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Equal("# Same", File.ReadAllText(doc.Path));
    }

    [Fact]
    public async Task Save_ConcurrentSavesFromTheSameBaseline_ExactlyOneWinsTheOtherConflicts()
    {
        using var doc = new TempDocument("# Base");
        var session = new LiveSession(doc.Path, "edit");
        var ready = new Barrier(2);

        string SaveFrom(string source)
        {
            ready.SignalAndWait();
            return session.Save(new SaveInput(source, ExpectedBaseline: "# Base"));
        }

        var first = Task.Run(() => SaveFrom("# First"));
        var second = Task.Run(() => SaveFrom("# Second"));
        var results = await Task.WhenAll(first, second);

        // The exclusive compare-and-write serializes the two writes: whichever commits first is
        // the baseline the other now compares against, so exactly one saves and the other sees a
        // conflict — the lost update the non-atomic read-then-write allowed can no longer happen.
        Assert.Single(results, json => json.Contains("\"outcome\":\"saved\""));
        Assert.Single(results, json => json.Contains("\"outcome\":\"conflict\""));

        var winner = results[0].Contains("\"outcome\":\"saved\"") ? "# First" : "# Second";
        Assert.Equal(winner, File.ReadAllText(doc.Path));
    }

    [Fact]
    public async Task SaveConfig_ConcurrentSavesFromTheSameBaseline_ExactlyOneWinsTheOtherConflicts()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var baseline = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", baseline);
        var session = ConfiguredSession(docPath, configPath);
        var ready = new Barrier(2);

        string SaveFrom(string source)
        {
            ready.SignalAndWait();
            return session.Save(new SaveInput(source, "config", baseline, "require-valid"));
        }

        var first = Task.Run(() => SaveFrom(Speaker("Bob", "B")));
        var second = Task.Run(() => SaveFrom(Speaker("Cara", "C")));
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, json => json.Contains("\"outcome\":\"saved\""));
        Assert.Single(results, json => json.Contains("\"outcome\":\"conflict\""));
    }

    [Fact]
    public void Refresh_ExternalChangeBackToSelfWrittenContent_StillBroadcasts()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# B", ExpectedBaseline: "# Old"));
        session.Refresh(); // the browser's own write is suppressed once, consuming the token
        Assert.False(reader.TryRead(out _));

        File.WriteAllText(doc.Path, "# A");
        session.Refresh();
        Assert.True(reader.TryRead(out var toA));
        Assert.Contains("# A", toA!.Data);

        // An external change back to the earlier self-written content is a real external edit now,
        // not a stale self-write: one-shot suppression means it still reloads.
        File.WriteAllText(doc.Path, "# B");
        session.Refresh();
        Assert.True(reader.TryRead(out var backToB));
        Assert.Contains("# B", backToB!.Data);
    }

    [Fact]
    public void Refresh_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        session.Refresh(); // the watcher firing for the browser's own write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Refresh_ExternalChangeAfterSave_StillBroadcasts()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        File.WriteAllText(doc.Path, "# External");
        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# External", received.Data);
    }

    [Fact]
    public void SaveConfig_ValidRequireValid_WritesAndRecompiles()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);

        var json = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", Speaker("Alice", "A"), "require-valid"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Contains("Bob", File.ReadAllText(configPath));
        Assert.Contains("\"name\":\"Bob\"", json);
    }

    [Fact]
    public void SaveConfig_InvalidRequireValid_ReturnsInvalidAutoAndWritesNothing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        var json = session.Save(new SaveInput(broken, "config", valid, "require-valid"));

        Assert.Contains("\"outcome\":\"invalid-auto\"", json);
        Assert.Equal(valid, File.ReadAllText(configPath)); // Auto never writes invalid TOML
    }

    [Fact]
    public void SaveConfig_InvalidAllowInvalid_WritesAndReturnsSavedInvalid()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        var json = session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));

        Assert.Contains("\"outcome\":\"saved-invalid\"", json);
        Assert.Equal(broken, File.ReadAllText(configPath)); // persisted, like a force-write
        Assert.Contains("bogus", json); // the payload carries the invalid source for the editor
    }

    [Fact]
    public void SaveConfig_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var disk = Speaker("External", "E");
        var configPath = tree.File("dialogue.toml", disk);
        var session = ConfiguredSession(docPath, configPath);

        var json = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", Speaker("Alice", "A"), "require-valid"));

        Assert.Contains("\"outcome\":\"conflict\"", json);
        Assert.Equal(disk, File.ReadAllText(configPath));
    }

    [Fact]
    public void SaveConfig_WithoutAConfigFile_Throws()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path, VisualizationMode.Edit);

        Assert.Throws<InvalidOperationException>(
            () => session.Save(new SaveInput(Speaker("Bob", "B"), "config")));
    }

    [Fact]
    public void Reload_Document_ReturnsLoadedWithTheDiskContent()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        File.WriteAllText(doc.Path, "# External");

        var json = session.Reload(null);

        Assert.Contains("\"outcome\":\"loaded\"", json);
        Assert.Contains("# External", json);
    }

    [Fact]
    public void Reload_DeletedDocument_ReturnsMissing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var session = new LiveSession(docPath, "edit");
        File.Delete(docPath);

        var json = session.Reload(null);

        Assert.Contains("\"outcome\":\"missing\"", json);
    }

    [Fact]
    public void Reload_InvalidConfig_KeepsLastValidAndCarriesTheDiskSource()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        File.WriteAllText(configPath, "[[speakers]]\nbogus = true\n");

        var json = session.Reload("config");

        Assert.Contains("\"outcome\":\"invalid\"", json);
        Assert.Contains("bogus", json); // the external invalid TOML is carried for the editor
        Assert.Contains("\"name\":\"Alice\"", json); // last valid report is retained
    }

    [Fact]
    public void CreateConfig_WritesTheStarterFileAndAdoptsIt()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit); // no configuration
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        var result = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Created, result.Status);
        Assert.True(File.Exists(configPath));
        Assert.Contains("[[speakers]]", File.ReadAllText(configPath));
        Assert.Contains("\"outcome\":\"saved\"", result.Payload);
        Assert.Equal(configPath, session.ConfigPath); // adopted: the session now applies it
        // Staged temp + atomic move: the created file is complete and no staging temp is left behind.
        Assert.Equal(
            new[] { "dialogue.toml", "scene.dialogue.md" },
            Directory.GetFiles(tree.Root).Select(Path.GetFileName).OrderBy(name => name).ToArray());
        // Adopted: a later baseline-checked save recompiles with the new speaker.
        var saved = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", ConfigStarter.Template, "require-valid"));
        Assert.Contains("\"name\":\"Bob\"", saved);
        Assert.Contains("Bob", File.ReadAllText(configPath));
    }

    [Fact]
    public void CreateConfig_ExistingTemplateOnDisk_AdoptsWithoutRewriting()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var configPath = tree.File("dialogue.toml", ConfigStarter.Template);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Adopted, result.Status);
        Assert.Contains("\"outcome\":\"saved\"", result.Payload);
        Assert.Contains("dialogue.toml", result.Payload); // the payload now carries the configuration file
        Assert.Equal(configPath, session.ConfigPath);
    }

    [Fact]
    public void CreateConfig_ExistingDifferentContent_ReturnsConflictAndLeavesItUntouched()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var configPath = tree.File("dialogue.toml", "# hand-written\n");
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Conflict, result.Status);
        Assert.Equal("# hand-written\n", File.ReadAllText(configPath)); // untouched
        Assert.Null(session.ConfigPath); // no config adopted
    }

    [Fact]
    public void CreateConfig_WriteFails_LeavesNoConfigStateAndRetrySucceeds()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var missingDirectory = Path.Combine(tree.Root, "nope", "dialogue.toml");

        // The containing folder does not exist, so the exclusive create throws before writing.
        Assert.Throws<DirectoryNotFoundException>(() => session.CreateConfig(missingDirectory));
        Assert.Null(session.ConfigPath); // the no-config state is unchanged

        // A retry at a valid path still succeeds: _configPath never mutated on the failed attempt.
        var result = session.CreateConfig(Path.Combine(tree.Root, "dialogue.toml"));
        Assert.Equal(CreateConfigStatus.Created, result.Status);
    }

    [Fact]
    public void CurrentDocumentJson_AfterSavedInvalidConfig_CarriesTheInvalidSourceAndStatus()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        var json = session.CurrentDocumentJson();

        // A page reload re-serializes the current document: it must restore the saved-invalid state
        // (the invalid source and a stale report), not silently revert to the last valid text.
        Assert.Contains("\"configStatus\":\"saved-invalid\"", json);
        Assert.Contains("bogus", json); // the persisted invalid TOML
        Assert.Contains("\"name\":\"Alice\"", json); // the last valid speakers remain (stale)
    }

    [Fact]
    public void RenderInitialHtml_AfterSavedInvalidConfig_CarriesTheInvalidSourceAndStatus()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        var html = session.RenderInitialHtml();

        Assert.Contains("\"configStatus\":\"saved-invalid\"", html);
        Assert.Contains("bogus", html);
    }

    [Fact]
    public void CurrentDocumentJson_AfterConfigBecomesValidAgain_DropsTheStaleOverlay()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        session.Save(new SaveInput(Speaker("Bob", "B"), "config", broken, "require-valid"));
        var json = session.CurrentDocumentJson();

        Assert.DoesNotContain("\"configStatus\"", json);
        Assert.Contains("\"name\":\"Bob\"", json);
    }

    [Fact]
    public void CreateConfig_RetryAfterAdopt_IsIdempotentWhileTheFileIsStillTheTemplate()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        var created = session.CreateConfig(configPath);
        Assert.Equal(CreateConfigStatus.Created, created.Status);

        // The first response was lost, so the client retries the same create. The session already
        // adopted the file, but it is still the untouched template, so the retry is idempotent.
        var retry = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Adopted, retry.Status);
        Assert.Contains("\"outcome\":\"saved\"", retry.Payload);
        Assert.Equal(configPath, session.ConfigPath);
    }

    [Fact]
    public void CreateConfig_RetryAfterAdopt_DifferingContent_ReturnsConflict()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        session.CreateConfig(configPath);
        File.WriteAllText(configPath, Speaker("Alice", "A")); // the config diverged from the template

        var retry = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Conflict, retry.Status);
        Assert.Equal(Speaker("Alice", "A"), File.ReadAllText(configPath)); // untouched
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
    public void RefreshConfig_ExternalChange_BroadcastsAReloadConfigWithTheDiskContent()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(configPath, Speaker("External", "E"));

        session.RefreshConfig();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload-config", received!.Event);
        Assert.Contains("External", received.Data);
    }

    [Fact]
    public void RefreshConfig_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput(Speaker("Bob", "B"), "config", valid, "require-valid"));
        session.RefreshConfig(); // the watcher firing for the browser's own config write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void RefreshConfig_ExternalChangeBackToSelfWrittenContent_StillBroadcasts()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        var bob = Speaker("Bob", "B");
        session.Save(new SaveInput(bob, "config", valid, "require-valid"));
        session.RefreshConfig(); // the browser's own config write is suppressed once
        Assert.False(reader.TryRead(out _));

        File.WriteAllText(configPath, Speaker("External", "E"));
        session.RefreshConfig();
        Assert.True(reader.TryRead(out var toExternal));
        Assert.Contains("External", toExternal!.Data);

        File.WriteAllText(configPath, bob);
        session.RefreshConfig();
        Assert.True(reader.TryRead(out var backToBob));
        Assert.Contains("Bob", backToBob!.Data);
    }

    [Fact]
    public void RefreshConfig_DeletedConfiguration_BroadcastsAProblemTargetingConfig()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.Delete(configPath);

        session.RefreshConfig();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("not found", received.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"target\":\"config\"", received.Data); // routes through the config controller
    }

    [Fact]
    public void RefreshConfig_WithoutAConfigFile_DoesNothing()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path, VisualizationMode.Edit);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.RefreshConfig();

        Assert.False(reader.TryRead(out _));
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
