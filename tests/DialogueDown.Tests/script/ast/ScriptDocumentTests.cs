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
        var body = new ScriptBlock[] { Line(Text("Welcome.")), SceneHeading("Greetings") };

        var document = new ScriptDocument(body, span);

        Assert.Equal(body, document.Body);
        Assert.Equal(span, document.Span);
        Assert.IsAssignableFrom<ScriptNode>(document);
    }

    [Fact]
    public void Constructor_HoldsContentAndHeadings_InOrder()
    {
        var intro = Line(Text("Before any heading."));
        var heading = SceneHeading("Under a heading");

        var document = new ScriptDocument([intro, heading], SourceSpanFactory.Span());

        Assert.Same(intro, document.Body[0]);
        Assert.Same(heading, document.Body[1]);
    }
}
