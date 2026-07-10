using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class DisplayGraphJsonTests
{
    [Fact]
    public void Serialize_UsesCamelCaseKeysAndStringEnum()
    {
        var graph = MakeGraph(
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
    public void Serialize_IncludesStageDescription()
    {
        var graph = MakeGraph(
            "Markdown AST", [Node("n0", "Document")], [], description: "What it shows.");

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"description\":\"What it shows.\"", json);
    }

    [Fact]
    public void Serialize_IncludesNodeSourceWhenPresentAndOmitsWhenNull()
    {
        var graph = MakeGraph(
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
        var graph = MakeGraph("G", [new DisplayNode("n0", "Code span", [], null, "call")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"category\":\"call\"", json);
    }

    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters_SoScriptCannotBreakOut()
    {
        var graph = MakeGraph("G", [Node("n0", "</script><b>&")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.DoesNotContain("</script>", json);
        Assert.Contains("\\u003C", json); // '<' is unicode-escaped
    }

    [Fact]
    public void SerializeReport_WrapsModeSourceAndStages()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hello", [graph]);

        Assert.Contains("\"mode\":\"static\"", json);
        Assert.Contains("\"source\":\"# Hello\"", json);
        Assert.Contains("\"stages\":[", json);
        Assert.Contains("\"title\":\"Markdown AST\"", json);
    }

    [Fact]
    public void SerializeReport_OmitsSourceWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, null, [graph]);

        Assert.DoesNotContain("\"source\":", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void SerializeReport_WithModeAndPath_AddsBoth()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("watch", "scene.dialogue.md", "# Hi", [graph]);

        Assert.Contains("\"mode\":\"watch\"", json);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hi\"", json);
    }

    [Fact]
    public void SerializeReport_WithoutPath_OmitsPath()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]);

        Assert.DoesNotContain("\"path\":", json);
    }

    [Fact]
    public void SerializeDocument_WrapsModePathSourceAndStages()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeDocument("watch", "scene.dialogue.md", "# Hi", [graph]);

        Assert.Contains("\"mode\":\"watch\"", json);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hi\"", json);
        Assert.Contains("\"stages\":[", json);
    }
}
