using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class DefaultSpeakerTests
{
    [Fact]
    public void Constructor_ExposesSpan_AndIsASpeaker()
    {
        var span = SourceSpanFactory.Span();

        var speaker = new DefaultSpeaker(span);

        Assert.Equal(span, speaker.Span);
        Assert.IsAssignableFrom<Speaker>(speaker);
        Assert.IsAssignableFrom<ScriptNode>(speaker);
    }
}
