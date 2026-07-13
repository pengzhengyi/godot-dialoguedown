using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Semantics.Errors;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.SpeakerSymbolAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SpeakerBinderTests
{
    [Fact]
    public void Bind_NameReference_ResolvesByName()
    {
        var table = Bind(SpeakerNameReference("Alice"));

        Assert.Equal("Alice", table.Resolve(SpeakerNameReference("Alice")).Name);
    }

    [Fact]
    public void Bind_IdReference_ResolvesById()
    {
        var table = Bind(SpeakerDeclaration("Alice", "A"), SpeakerIdReference("A"));

        Assert.Equal("A", table.Resolve(SpeakerIdReference("A")).Id);
    }

    [Fact]
    public void Bind_RepeatedNameReference_ConvergesOnOneSymbol()
    {
        var table = Bind(SpeakerNameReference("Alice"), SpeakerNameReference("Alice"));

        Assert.Same(
            table.Resolve(SpeakerNameReference("Alice")),
            table.Resolve(SpeakerNameReference("Alice")));
    }

    [Fact]
    public void Bind_Declaration_BindsNameAndIdToOneSymbol()
    {
        var table = Bind(SpeakerDeclaration("Alice", "A"));

        Assert.Same(
            table.Resolve(SpeakerNameReference("Alice")),
            table.Resolve(SpeakerIdReference("A")));
    }

    [Fact]
    public void Bind_Declaration_MergesCustomTags()
    {
        var table = Bind(SpeakerDeclaration("Alice", tags: CustomTag("happy")));

        AssertTags(table.Resolve(SpeakerNameReference("Alice")), ("happy", null));
    }

    [Fact]
    public void Bind_DefaultTag_MarksTheSpeakerDefault()
    {
        var table = Bind(SpeakerDeclaration("Narrator", tags: ReservedTag("default")));

        var narrator = table.Resolve(SpeakerNameReference("Narrator"));
        Assert.True(narrator.IsDefault);
        Assert.Same(narrator, table.Resolve(DefaultSpeaker()));
    }

    [Fact]
    public void Bind_OtherReservedTag_IsLeftForTheTagValidator()
    {
        var table = Bind(SpeakerDeclaration("Alice", tags: ReservedTag("vip")));

        var alice = table.Resolve(SpeakerNameReference("Alice"));
        Assert.False(alice.IsDefault);
        Assert.Empty(alice.Tags); // a reserved tag is not a custom tag; the binder ignores it
    }

    [Fact]
    public void Bind_IdReferencedBeforeItsDeclaration_GetsNamed()
    {
        var table = Bind(SpeakerIdReference("A"), SpeakerDeclaration("Alice", "A"));

        Assert.Equal("Alice", table.Resolve(SpeakerIdReference("A")).Name);
    }

    [Fact]
    public void Bind_NameReferencedBeforeItsDeclaration_GetsItsId()
    {
        var table = Bind(SpeakerNameReference("Alice"), SpeakerDeclaration("Alice", "A"));

        Assert.Equal("A", table.Resolve(SpeakerNameReference("Alice")).Id);
        Assert.Same(
            table.Resolve(SpeakerNameReference("Alice")),
            table.Resolve(SpeakerIdReference("A")));
    }

    [Fact]
    public void Bind_PartialDeclaration_MergesTagsIntoTheIdSymbol()
    {
        var table = Bind(
            SpeakerDeclaration("Alice", "A", CustomTag("calm")),
            new PartialSpeakerDeclaration("A", [CustomTag("excited")], SourceSpanFactory.Span()));

        var names = table.Resolve(SpeakerIdReference("A")).Tags.Select(tag => tag.Name);
        Assert.Equal(["calm", "excited"], names.OrderBy(name => name));
    }

    [Fact]
    public void Bind_NoDeclaredDefault_FallsBackToAnAnonymousDefault()
    {
        var table = Bind(SpeakerNameReference("Alice"));

        var fallback = table.Resolve(DefaultSpeaker());
        Assert.Null(fallback.Name);
        Assert.True(fallback.IsDefault);
    }

    [Fact]
    public void Bind_DefaultSpeakerNode_IsIgnored()
    {
        var table = Bind(DefaultSpeaker());

        Assert.Null(table.Resolve(DefaultSpeaker()).Name);
    }

    [Fact]
    public void Add_UnknownSpeakerKind_Throws()
    {
        var binder = new SpeakerBinder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => binder.Add(new UnknownSpeaker(SourceSpanFactory.Span())));
    }

    [Fact]
    public void Bind_NameBoundToTwoIds_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(
            () => Bind(SpeakerDeclaration("Alice", "A"), SpeakerDeclaration("Alice", "B")));

        Assert.Contains("@B", error.Message);
    }

    [Fact]
    public void Bind_IdBoundToTwoNames_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(
            () => Bind(SpeakerDeclaration("Alice", "A"), SpeakerDeclaration("Bob", "A")));

        Assert.Contains("Alice", error.Message);
    }

    [Fact]
    public void Bind_FusingTwoSeparatelyUsedSpeakers_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(() => Bind(
            SpeakerNameReference("Alice"),
            SpeakerIdReference("A"),
            SpeakerDeclaration("Alice", "A")));

        Assert.Contains("ambiguous", error.Message);
    }

    [Fact]
    public void Bind_TwoDefaultSpeakers_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(() => Bind(
            SpeakerDeclaration("Alice", tags: ReservedTag("default")),
            SpeakerDeclaration("Bob", tags: ReservedTag("default"))));

        Assert.Contains("only one default speaker", error.Message);
    }

    [Fact]
    public void Bind_SameSpeakerMarkedDefaultTwice_IsAllowed()
    {
        var table = Bind(
            SpeakerDeclaration("Alice", "A", ReservedTag("default")),
            new PartialSpeakerDeclaration("A", [ReservedTag("default")], SourceSpanFactory.Span()));

        Assert.True(table.Resolve(SpeakerNameReference("Alice")).IsDefault);
    }

    [Fact]
    public void Bind_TwoDefaults_LabelsAnIdOnlyDefaultByItsId()
    {
        // The first default is an @id partial (no name yet); the message names both the
        // established default (@A) and the offending one (Bob), rendering @A via ToString.
        var error = Assert.Throws<DialogueSemanticError>(() => Bind(
            new PartialSpeakerDeclaration("A", [ReservedTag("default")], SourceSpanFactory.Span()),
            SpeakerDeclaration("Bob", tags: ReservedTag("default"))));

        Assert.Contains("@A", error.Message);
        Assert.Contains("Bob", error.Message);
    }

    [Fact]
    public void Bind_IdNeverGivenAName_ViolatesTheNameInvariant()
    {
        var reference = new SpeakerIdReference("A", new SourceSpan(7, 2));

        var error = Assert.Throws<DialogueSemanticError>(() => Bind(reference));

        Assert.Contains("@A", error.Message);
        Assert.Equal(reference.Span, error.Span); // points at where @A was first used
    }

    private static SpeakerTable Bind(params Speaker[] speakers) => SpeakerBinder.Bind(speakers);

    private sealed record UnknownSpeaker(SourceSpan Span) : Speaker(Span);
}
