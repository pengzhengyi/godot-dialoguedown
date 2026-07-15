using DialogueDown.Configuration;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredSpeakerReaderTests
{
    private const string SourceName = "dialogue.toml";

    [Fact]
    public void Read_EmptyDocument_ReturnsEmpty()
    {
        Assert.Empty(Read(string.Empty));
    }

    [Fact]
    public void Read_NonSpeakerTable_IsIgnored()
    {
        var speakers = Read("""
            [compiler]
            note = "not a speaker"
            """);

        Assert.Empty(speakers);
    }

    [Fact]
    public void Read_UnrelatedTableArray_IsIgnored()
    {
        var speakers = Read("""
            [[extras]]
            note = "not a speaker"

            [[speakers]]
            name = "Alice"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Read_NameAndId_AreMapped()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            id = "A"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
        Assert.Empty(speaker.CustomTags);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_SpeakerWithoutId_LeavesIdNull()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Null(speaker.Id);
    }

    [Fact]
    public void Read_DefaultTrue_AddsDefaultReservedTag()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"
            default = true
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag reserved = Assert.Single(speaker.ReservedTags);
        Assert.Equal(new ConfiguredTag(ReservedTagNames.Default), reserved);
    }

    [Fact]
    public void Read_DefaultFalse_AddsNoReservedTag()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            default = false
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_ShorthandTagWithoutValue_MapsNameOnly()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = ["main"]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("main"), tag);
    }

    [Fact]
    public void Read_ShorthandTagWithValue_SplitsAtFirstEquals()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = ["mood=happy=ish"]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("mood", "happy=ish"), tag);
    }

    [Fact]
    public void Read_InlineTableTag_MapsNameAndValue()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro", value = "ok" }]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("quest=intro", "ok"), tag);
    }

    [Fact]
    public void Read_InlineTableTagWithoutValue_LeavesValueNull()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro" }]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("quest=intro"), tag);
    }

    [Fact]
    public void Read_MixedTagForms_PreserveArrayOrder()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = ["main", "mood=happy", { name = "role", value = "guide" }]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal(
            new[]
            {
                new ConfiguredTag("main"),
                new ConfiguredTag("mood", "happy"),
                new ConfiguredTag("role", "guide"),
            },
            speaker.CustomTags);
    }

    [Fact]
    public void Read_MultipleSpeakers_PreserveDocumentOrder()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"

            [[speakers]]
            name = "Alice"
            """);

        Assert.Equal(new[] { "Narrator", "Alice" }, speakers.Select(s => s.Name));
    }

    private static IReadOnlyList<ConfiguredSpeaker> Read(string toml)
    {
        var document = new TomlDocumentParser(SourceName).Parse(toml);
        return new ConfiguredSpeakerReader().Read(document);
    }
}
