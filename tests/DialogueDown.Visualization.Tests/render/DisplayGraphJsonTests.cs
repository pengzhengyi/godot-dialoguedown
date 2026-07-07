using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class DisplayGraphJsonTests
{
    [Fact]
    public void Serialize_UsesCamelCaseKeysAndStringEnum()
    {
        var graph = Graph(
            "Markdown AST",
            [Node("n0", "Document"), Node("n1", "Heading", Attr("level", "2"))],
            [Child("n0", "n1")]);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"title\":\"Markdown AST\"", json);
        Assert.Contains("\"nodes\":", json);
        Assert.Contains("\"label\":\"Heading\"", json);
        Assert.Contains("\"attributes\":[{\"name\":\"level\",\"value\":\"2\"}]", json);
        Assert.Contains("\"fromId\":\"n0\"", json);
        Assert.Contains("\"kind\":\"Child\"", json);
    }

    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters_SoScriptCannotBreakOut()
    {
        var graph = Graph("G", [Node("n0", "</script><b>&")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.DoesNotContain("</script>", json);
        Assert.Contains("\\u003C", json); // '<' is unicode-escaped
    }
}
