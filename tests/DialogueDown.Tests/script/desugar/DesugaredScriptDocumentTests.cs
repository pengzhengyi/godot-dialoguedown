using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DesugaredScriptDocumentTests
{
    [Fact]
    public void Body_SurfacesTheWrappedDocumentBody()
    {
        var document = new ScriptDocument([Line(Text("hi"))]);

        var desugared = new DesugaredScriptDocument(document);

        Assert.Same(document, desugared.Document);
        Assert.Same(document.Body, desugared.Body);
    }
}
