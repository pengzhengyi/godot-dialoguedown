using DialogueDown.Compilation;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Compilation;

public sealed class ScriptCompilerFactoryTests
{
    [Fact]
    public void CreateDefault_CompilesAScriptThroughEveryStage()
    {
        var source =
            """
            # Scene

            Alice: Ready? => [Play](#scene)

            No speaker here.
            """;

        var result = ScriptCompilerFactory.CreateDefault().Compile(source);

        Assert.Equal(source, result.Source);
        Assert.NotNull(result.Markdown);
        Assert.NotNull(result.Script);

        // The desugared tree upholds the post-desugar invariants: the jump is assembled
        // and the speaker-less line has a default speaker.
        var body = result.Desugared.Body;
        AssertSceneHeading(body[0], "Scene", 1);

        var spoken = AssertLine(body[1]);
        AssertSpeakerNameReference(spoken.Speaker!, "Alice");
        AssertJump(spoken.Speech[^1], "#scene");

        var silent = AssertLine(body[2]);
        AssertDefaultSpeaker(silent.Speaker);

        // The semantic model is bound: the heading became a scene the jump resolves to.
        Assert.True(result.Semantics.Anchors.TryResolve("scene", out _));
        Assert.IsType<SceneJump>(Assert.Single(result.Semantics.Jumps.Resolutions));
    }
}
