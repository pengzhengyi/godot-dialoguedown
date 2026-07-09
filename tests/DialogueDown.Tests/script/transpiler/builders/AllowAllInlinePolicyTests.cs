using DialogueDown.Script.Transpiler.Builders;
using Md = DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class AllowAllInlinePolicyTests
{
    private static readonly AllowAllInlinePolicy _policy = AllowAllInlinePolicy.Instance;

    [Fact]
    public void Supports_EveryInline()
    {
        Assert.True(_policy.Supports(Md.Text("hi")));
        Assert.True(_policy.Supports(Md.CodeSpan("q")));
        Assert.True(_policy.Supports(Md.Link("#x", Md.Text("l"))));
    }

    [Fact]
    public void SupportsJumps_IsTrue() => Assert.True(_policy.SupportsJumps);

    [Fact]
    public void Resolve_IsUnreachable_AndThrows() =>
        Assert.Throws<InvalidOperationException>(() => _policy.Resolve(Md.CodeSpan("q")));
}
