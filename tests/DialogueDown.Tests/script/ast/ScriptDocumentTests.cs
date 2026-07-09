using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ScriptDocumentTests
{
    [Fact]
    public void Constructor_ExposesBody_AndIsNotASpannedNode()
    {
        var body = new ScriptBlock[] { Line(Text("Welcome.")), SceneHeading("Greetings") };

        var document = new ScriptDocument(body);

        Assert.Equal(body, document.Body);
        Assert.IsNotAssignableFrom<ScriptNode>(document);
    }

    [Fact]
    public void Constructor_HoldsContentAndHeadings_InOrder()
    {
        var intro = Line(Text("Before any heading."));
        var heading = SceneHeading("Under a heading");

        var document = new ScriptDocument([intro, heading]);

        Assert.Same(intro, document.Body[0]);
        Assert.Same(heading, document.Body[1]);
    }
}
