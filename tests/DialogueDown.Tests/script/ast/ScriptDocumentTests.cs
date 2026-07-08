using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ScriptDocumentTests
{
    [Fact]
    public void Constructor_ExposesBodyAndSpan_AndIsAScriptNode()
    {
        var span = SourceSpanFactory.Span();
        var body = new Block[] { Line(Text("Welcome.")), Scene(Line(Text("Hello, Bob!"))) };

        var document = new ScriptDocument(body, span);

        Assert.Equal(body, document.Body);
        Assert.Equal(span, document.Span);
        Assert.IsAssignableFrom<ScriptNode>(document);
    }

    [Fact]
    public void Constructor_HoldsPreHeadingContentThenScenes_InOrder()
    {
        var intro = Line(Text("Before any heading."));
        var scene = Scene(Line(Text("Under a heading.")));

        var document = new ScriptDocument([intro, scene], SourceSpanFactory.Span());

        Assert.Same(intro, document.Body[0]);
        Assert.Same(scene, document.Body[1]);
    }
}
