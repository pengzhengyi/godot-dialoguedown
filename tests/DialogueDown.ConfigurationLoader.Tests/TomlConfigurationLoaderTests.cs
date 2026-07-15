using DialogueDown.Configuration;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlConfigurationLoaderTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsDefaultOptions()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse(string.Empty);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Parse_ConfigWithoutSpeakers_ReturnsDefaultOptions()
    {
        var toml = """
            [compiler]
            note = "no speakers here"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Parse_IgnoresUnrelatedTableArrays()
    {
        var toml = """
            [[extras]]
            note = "not a speaker"

            [[speakers]]
            name = "Alice"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Parse_SingleSpeaker_MapsNameAndId()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            id = "A"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
        Assert.Empty(speaker.CustomTags);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Parse_SpeakerWithoutId_LeavesIdNull()
    {
        var toml = """
            [[speakers]]
            name = "Narrator"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Null(speaker.Id);
    }

    [Fact]
    public void Parse_DefaultTrue_AddsDefaultReservedTag()
    {
        var toml = """
            [[speakers]]
            name = "Narrator"
            default = true
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        ConfiguredTag reserved = Assert.Single(speaker.ReservedTags);
        Assert.Equal(new ConfiguredTag(ReservedTagNames.Default), reserved);
    }

    [Fact]
    public void Parse_DefaultFalse_AddsNoReservedTag()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            default = false
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Parse_CustomTags_MapsShorthandAndInlineTableForms()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            tags = ["main", "mood=happy", { name = "quest=intro", value = "ok" }]
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Equal(
            new[]
            {
                new ConfiguredTag("main"),
                new ConfiguredTag("mood", "happy"),
                new ConfiguredTag("quest=intro", "ok"),
            },
            speaker.CustomTags);
    }

    [Fact]
    public void Parse_MultipleSpeakers_PreservesDocumentOrder()
    {
        var toml = """
            [[speakers]]
            name = "Narrator"

            [[speakers]]
            name = "Alice"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml);

        Assert.Equal(new[] { "Narrator", "Alice" }, options.Speakers.Select(s => s.Name));
    }

    [Fact]
    public void Load_ReadsFileAndParses()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            id = "A"
            """;
        string path = Path.Combine(Path.GetTempPath(), $"dialogue-{Guid.NewGuid():N}.toml");
        File.WriteAllText(path, toml);

        try
        {
            CompilerOptions options = TomlConfigurationLoader.Load(path);

            ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
            Assert.Equal("Alice", speaker.Name);
            Assert.Equal("A", speaker.Id);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
