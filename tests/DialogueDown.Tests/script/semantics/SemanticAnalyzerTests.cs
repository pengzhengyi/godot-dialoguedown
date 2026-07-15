using DialogueDown.Configuration;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Semantics.Errors;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.ConfigurationFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SemanticAnalyzerTests
{
    private readonly SemanticAnalyzer _analyzer = new(new SemanticAnalyzerOptions([]));

    [Fact]
    public void Analyze_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _analyzer.Analyze(null!, "source"));

    [Fact]
    public void Analyze_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => _analyzer.Analyze(new DesugaredScriptDocument(new ScriptDocument([])), null!));

    [Fact]
    public void Analyze_HoldsTheDesugaredTree()
    {
        var desugared = Pipeline.UntilDesugared("Alice: Hi.");

        var model = _analyzer.Analyze(desugared, "Alice: Hi.");

        Assert.Same(desugared, model.Desugared);
    }

    [Fact]
    public void Analyze_BindsSpeakers()
    {
        var model = Analyze("Alice @A #happy ##default: Ready?");

        var alice = model.Speakers.Resolve(new SpeakerNameReference("Alice", SourceSpanFactory.Span()));
        Assert.Equal("Alice", alice.Name);
        Assert.Equal("A", alice.Id);
        Assert.True(alice.IsDefault);
        Assert.Contains(alice.Tags, tag => tag.Name == "happy");
    }

    [Fact]
    public void Analyze_ConfiguredDefaultSpeaker_ResolvesSpeakerlessLines()
    {
        var analyzer = new SemanticAnalyzer(new SemanticAnalyzerOptions([DefaultConfiguredSpeaker("Narrator")]));

        var model = analyzer.Analyze(Pipeline.UntilDesugared("Hi."), "Hi.");

        var speaker = model.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span()));
        Assert.Equal("Narrator", speaker.Name);
        Assert.True(speaker.IsDefault);
    }

    [Fact]
    public void Analyze_InFileDefault_OverridesTheConfiguredDefault()
    {
        var analyzer = new SemanticAnalyzer(new SemanticAnalyzerOptions([DefaultConfiguredSpeaker("Narrator")]));
        var source = "Bob ##default: Hi.";

        var model = analyzer.Analyze(Pipeline.UntilDesugared(source), source);

        Assert.Equal("Bob", model.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span())).Name);
    }

    [Fact]
    public void Analyze_BuildsTheSceneTreeAndAnchors()
    {
        var model = Analyze(
            """
            ## Play tennis

            Alice: Ready?
            """);

        var scene = Assert.Single(model.SceneRoot.Children);
        Assert.Equal("play-tennis", scene.Anchor);
        Assert.True(model.Anchors.TryResolve("play-tennis", out _));
    }

    [Fact]
    public void Analyze_ResolvesASameFileJumpToItsScene()
    {
        var model = Analyze(
            """
            ## Play tennis

            Alice: Ready? => [Go](#play-tennis)
            """);

        var resolution = Assert.Single(model.Jumps.Resolutions);
        var sceneJump = Assert.IsType<SceneJump>(resolution);
        Assert.Equal("play-tennis", sceneJump.Scene.Anchor);
    }

    [Fact]
    public void Analyze_PropagatesAMissingJumpTarget_AfterBuildingAnchors()
    {
        var error = Assert.Throws<DialogueSemanticError>(() => Analyze(
            """
            ## Play tennis

            Alice: Ready? => [Go](#no-such-scene)
            """));

        Assert.Contains("#no-such-scene", error.Message);
    }

    [Fact]
    public void Analyze_ValidatesReservedTags()
    {
        var error = Assert.Throws<DialogueSemanticError>(() => Analyze("Alice ##bogus: Hi."));

        Assert.Contains("##bogus", error.Message);
    }

    private SemanticModel Analyze(string source) => _analyzer.Analyze(Pipeline.UntilDesugared(source), source);
}
