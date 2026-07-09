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
    public void Serialize_IncludesNodeSourceWhenPresentAndOmitsWhenNull()
    {
        var graph = Graph(
            "G",
            [new DisplayNode("n0", "Text", [], "# Hi"), Node("n1", "Empty")],
            []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"source\":\"# Hi\"", json);
        var sourceKeyCount = json.Split("\"source\":").Length - 1;
        Assert.Equal(1, sourceKeyCount);
    }

    [Fact]
    public void Serialize_IncludesNodeCategoryWhenPresent()
    {
        var graph = Graph("G", [new DisplayNode("n0", "Code span", [], null, "call")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"category\":\"call\"", json);
    }

    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters_SoScriptCannotBreakOut()
    {
        var graph = Graph("G", [Node("n0", "</script><b>&")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.DoesNotContain("</script>", json);
        Assert.Contains("\\u003C", json); // '<' is unicode-escaped
    }

    [Fact]
    public void SerializeReport_WrapsSourceAndStages()
    {
        var graph = Graph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("# Hello", [graph]);

        Assert.Contains("\"source\":\"# Hello\"", json);
        Assert.Contains("\"stages\":[", json);
        Assert.Contains("\"title\":\"Markdown AST\"", json);
    }

    [Fact]
    public void SerializeReport_OmitsSourceWhenNull()
    {
        var graph = Graph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport(null, [graph]);

        Assert.DoesNotContain("\"source\":", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void SerializeReport_WithLivePath_AddsLiveMarkerAndPath()
    {
        var graph = Graph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("# Hi", [graph], "scene.dialogue.md");

        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"live\":true", json);
        Assert.Contains("\"source\":\"# Hi\"", json);
    }

    [Fact]
    public void SerializeReport_WithoutLivePath_HasNoLiveMarkerOrPath()
    {
        var graph = Graph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("# Hi", [graph]);

        Assert.DoesNotContain("\"live\":", json);
        Assert.DoesNotContain("\"path\":", json);
    }

    [Fact]
    public void SerializeDocument_WrapsPathSourceAndStages()
    {
        var graph = Graph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeDocument("scene.dialogue.md", "# Hi", [graph]);

        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hi\"", json);
        Assert.Contains("\"stages\":[", json);
        Assert.DoesNotContain("\"live\":", json);
    }
}
