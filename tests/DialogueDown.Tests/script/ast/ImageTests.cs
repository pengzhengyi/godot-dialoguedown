using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ImageTests
{
    [Fact]
    public void Constructor_ExposesSourceAltAndSpan_AndIsASpeechFragment()
    {
        var span = SourceSpanFactory.Span();
        var alt = new SpeechFragment[] { Text("a curious cat"), CustomTag("cute") };

        var image = new Image("cat.png", alt, span);

        Assert.Equal("cat.png", image.Source);
        Assert.Equal(alt, image.Alt);
        Assert.Equal(span, image.Span);
        Assert.IsAssignableFrom<SpeechFragment>(image);
        Assert.IsAssignableFrom<ScriptNode>(image);
    }

    [Fact]
    public void Constructor_AllowsEmptyAlt() =>
        Assert.Empty(new Image("cat.png", [], SourceSpanFactory.Span()).Alt);

    [Fact]
    public void Constructor_AllowsEmptySource_LeavingItUnresolved() =>
        // An empty source is a valid syntactic form; the semantic analyzer judges it.
        Assert.Equal(string.Empty, new Image(string.Empty, [], SourceSpanFactory.Span()).Source);

    [Fact]
    public void Constructor_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new Image(null!, [], SourceSpanFactory.Span()));
}
