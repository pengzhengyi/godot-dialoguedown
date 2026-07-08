using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class LineTests
{
    [Fact]
    public void Constructor_ExposesSpeakerSpeechAndSpan_AndIsABlock()
    {
        var span = SourceSpanFactory.Span();
        var speaker = SpeakerNameReference("Alice");
        var speech = new InlineFragment[] { Text("Hello, Bob!") };

        var line = new Line(speaker, speech, span);

        Assert.Same(speaker, line.Speaker);
        Assert.Equal(speech, line.Speech);
        Assert.Equal(span, line.Span);
        Assert.IsAssignableFrom<Block>(line);
        Assert.IsAssignableFrom<ScriptNode>(line);
    }

    [Fact]
    public void Constructor_AllowsNoSpeaker_ForADefaultToFillLater() =>
        Assert.Null(new Line(Speaker: null, [Text("Hi")], SourceSpanFactory.Span()).Speaker);

    [Fact]
    public void Constructor_AllowsEmptySpeech_WhenTheLineOnlyDeclaresASpeaker() =>
        Assert.Empty(new Line(SpeakerNameReference("Alice"), [], SourceSpanFactory.Span()).Speech);
}
