using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Errors;
using DialogueDown.Tests.Support;

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
    public void NotAGameCall_ThrowsAtTheSpan_WithTheTechnicalReason()
    {
        var span = SourceSpanFactory.Span(7, 3);

        var error = Assert.Throws<DialogueSyntaxError>(() => Build("just some words", span));

        Assert.Equal(span, error.Span);
        Assert.Contains("is not a game call", error.Message);
        Assert.Contains("↳", error.Message); // the grammar's technical reason is appended
    }

    [Fact]
    public void TrailingText_AfterAValidCall_IsRejected()
    {
        // The grammar matches the query, but the whole text is not consumed.
        var error = Assert.Throws<DialogueSyntaxError>(() => Build("\"key\" and more"));

        Assert.Contains("is not a game call", error.Message);
    }

    private static GameCall Build(string content, SourceSpan? span = null) =>
        _builder.Build(ParseInputFactory.Input(content), span ?? SourceSpanFactory.Span());
}
