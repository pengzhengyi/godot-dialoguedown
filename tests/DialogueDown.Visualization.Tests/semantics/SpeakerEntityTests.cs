using DialogueDown.Script.Semantics;
using DialogueDown.Visualization.Semantics;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SpeakerEntityTests
{
    [Fact]
    public void Key_PrefersTheId()
    {
        var symbol = SpeakerSymbol.ForName("Guide");
        symbol.GiveId("guide");
        Assert.Equal("speaker:@guide", SpeakerEntity.Key(symbol));
    }

    [Fact]
    public void Key_FallsBackToTheName() =>
        Assert.Equal("speaker:Alice", SpeakerEntity.Key(SpeakerSymbol.ForName("Alice")));

    [Fact]
    public void Key_IsDefaultForTheDefaultSpeaker()
    {
        var symbol = SpeakerSymbol.Anonymous();
        symbol.MarkDefault();
        Assert.Equal("speaker:(default)", SpeakerEntity.Key(symbol));
    }
}
