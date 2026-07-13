using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SpeakerTableTests
{
    [Fact]
    public void Resolve_Declaration_ReturnsItsSymbol()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(alice, table.Resolve(SpeakerDeclaration("Alice", "A")));
    }

    [Fact]
    public void Resolve_NameReference_ReturnsTheSymbolByName()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(alice, table.Resolve(SpeakerNameReference("Alice")));
    }

    [Fact]
    public void Resolve_IdReference_ReturnsTheSymbolById()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(alice, table.Resolve(SpeakerIdReference("A")));
    }

    [Fact]
    public void Resolve_PartialDeclaration_ReturnsTheSymbolById()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(
            alice,
            table.Resolve(new PartialSpeakerDeclaration("A", [], SourceSpanFactory.Span())));
    }

    [Fact]
    public void Resolve_DefaultSpeaker_ReturnsTheProvidedDefault()
    {
        var fallback = SpeakerSymbol.ForName("Narrator");
        var table = new SpeakerTable(
            new Dictionary<string, SpeakerSymbol>(),
            new Dictionary<string, SpeakerSymbol>(),
            fallback);

        Assert.Same(fallback, table.Resolve(DefaultSpeaker()));
    }

    [Fact]
    public void Resolve_UnknownSpeakerKind_Throws()
    {
        var table = Table(Alice());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => table.Resolve(new UnknownSpeaker(SourceSpanFactory.Span())));
    }

    private static SpeakerSymbol Alice()
    {
        var alice = SpeakerSymbol.ForName("Alice");
        alice.GiveId("A");
        return alice;
    }

    private static SpeakerTable Table(params SpeakerSymbol[] symbols)
    {
        var byName = symbols.Where(symbol => symbol.Name is not null)
            .ToDictionary(symbol => symbol.Name!, symbol => symbol);
        var byId = symbols.Where(symbol => symbol.Id is not null)
            .ToDictionary(symbol => symbol.Id!, symbol => symbol);
        return new SpeakerTable(byName, byId, SpeakerSymbol.ForName("Default"));
    }

    private sealed record UnknownSpeaker(SourceSpan Span) : Speaker(Span);
}
