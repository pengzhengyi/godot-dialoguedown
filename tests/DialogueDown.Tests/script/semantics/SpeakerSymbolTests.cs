using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.SpeakerSymbolAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SpeakerSymbolTests
{
    [Fact]
    public void ForName_KnowsOnlyTheName()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        Assert.Equal("Alice", symbol.Name);
        Assert.Null(symbol.Id);
    }

    [Fact]
    public void ForId_KnowsOnlyTheId()
    {
        var symbol = SpeakerSymbol.ForId("A");

        Assert.Equal("A", symbol.Id);
        Assert.Null(symbol.Name);
    }

    [Fact]
    public void NewSymbol_HasNoTagsAndIsNotDefault()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        Assert.Empty(symbol.Tags);
        Assert.False(symbol.IsDefault);
    }

    [Fact]
    public void GiveName_NamesAnIdOnlySymbol()
    {
        var symbol = SpeakerSymbol.ForId("A");

        symbol.GiveName("Alice");

        Assert.Equal("Alice", symbol.Name);
        Assert.Equal("A", symbol.Id);
    }

    [Fact]
    public void GiveId_IdsANameOnlySymbol()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.GiveId("A");

        Assert.Equal("A", symbol.Id);
        Assert.Equal("Alice", symbol.Name);
    }

    [Fact]
    public void MarkDefault_MakesItTheDefault()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MarkDefault();

        Assert.True(symbol.IsDefault);
    }

    [Fact]
    public void MergeTags_AddsTheGivenTags()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("happy", null, 0), TagAt("mood", "calm", 1)]);

        AssertTags(symbol, ("happy", null), ("mood", "calm"));
    }

    [Fact]
    public void MergeTags_SkipsATagWithTheSameIdentity()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("happy", null, 0)]);
        symbol.MergeTags([TagAt("happy", null, 5), TagAt("excited", null, 1)]);

        AssertTags(symbol, ("happy", null), ("excited", null));
    }

    [Fact]
    public void MergeTags_KeepsSameNameWithADifferentValue()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("mood", "happy", 0), TagAt("mood", "sad", 1)]);

        AssertTags(symbol, ("mood", "happy"), ("mood", "sad"));
    }

    [Fact]
    public void Tags_AreOrderedBySpanStart_NotByInsertionOrder()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("second", null, 10), TagAt("first", null, 0)]);

        AssertTags(symbol, ("first", null), ("second", null));
    }

    private static CustomTag TagAt(string name, string? value, int start) =>
        new(name, value, new SourceSpan(start, 1));
}
