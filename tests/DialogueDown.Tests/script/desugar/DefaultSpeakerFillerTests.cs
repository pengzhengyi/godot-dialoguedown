using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DefaultSpeakerFillerTests
{
    [Fact]
    public void Fill_LineWithoutSpeaker_GetsDefaultSpeaker()
    {
        var line = Line(Text("Hello"));

        var filled = DefaultSpeakerFiller.Fill(line);

        var speaker = Assert.IsType<DefaultSpeaker>(filled.Speaker);
        Assert.Equal(SourceSpan.EmptyAt(line.Span.Start), speaker.Span);
        Assert.True(speaker.Span.IsEmpty);
        Assert.Same(line.Speech, filled.Speech);
    }

    [Fact]
    public void Fill_LineWithSpeaker_IsReturnedUnchanged()
    {
        var line = new Line(
            SpeakerNameReference("Alice"), [Text("Hello")], SourceSpanFactory.Span());

        Assert.Same(line, DefaultSpeakerFiller.Fill(line));
    }

    [Fact]
    public void Fill_LoneCommandLine_GetsDefaultSpeaker()
    {
        // A silent command is a speaker-less line whose speech is a command; the same
        // fill covers it, with no special case.
        var line = Line(new DefaultCommand("wave", SourceSpanFactory.Span()));

        Assert.IsType<DefaultSpeaker>(DefaultSpeakerFiller.Fill(line).Speaker);
    }
}
