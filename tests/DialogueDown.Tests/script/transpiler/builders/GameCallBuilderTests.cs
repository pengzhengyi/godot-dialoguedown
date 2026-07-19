using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class GameCallBuilderTests
{
    private static readonly GameCallBuilder _builder =
        TranspilerBuilderFactory.GameCallBuilder();

    [Fact]
    public void Query_BecomesAQueryNode_WithTheGivenSpan()
    {
        var span = SourceSpanFactory.Span(2, 5);

        var call = Build("\"Alice.FavoriteColor\"", span);

        var query = Assert.IsType<Query>(call);
        Assert.Equal("Alice.FavoriteColor", query.Key);
        Assert.Equal(span, query.Span);
    }

    [Fact]
    public void DefaultCommand_BecomesADefaultCommandNode() =>
        Assert.Equal("Alice joins Art",
            Assert.IsType<DefaultCommand>(Build("""("Alice joins Art")""")).Action);

    [Fact]
    public void CustomCommand_BecomesACustomCommandNode()
    {
        var command = Assert.IsType<CustomCommand>(Build("""JoinClub("Alice", "Art")"""));

        Assert.Equal("JoinClub", command.Name);
        Assert.Equal(["Alice", "Art"], command.Args);
    }

    [Fact]
    public void NotAGameCall_ReportsAtTheSpan_AndRecoversToText()
    {
        var span = SourceSpanFactory.Span(7, 3);

        var fragment = Build("just some words", out var diagnostics, span);

        Assert.Equal("just some words", Assert.IsType<Text>(fragment).Content);
        Assert.Equal(span, AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.NotAGameCall).Span);
    }

    [Fact]
    public void TrailingText_AfterAValidCall_IsRejected()
    {
        // The grammar matches the query, but the whole text is not consumed.
        var fragment = Build("\"key\" and more", out var diagnostics);

        Assert.IsType<Text>(fragment);
        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.NotAGameCall);
    }

    private static InlineFragment Build(string content, SourceSpan? span = null) =>
        _builder.Build(
            ParseInputFactory.Input(content), span ?? SourceSpanFactory.Span(), new DiagnosticBag());

    private static InlineFragment Build(
        string content, out DiagnosticBag diagnostics, SourceSpan? span = null)
    {
        diagnostics = new DiagnosticBag();
        return _builder.Build(ParseInputFactory.Input(content), span ?? SourceSpanFactory.Span(), diagnostics);
    }
}
