using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for <see cref="IInlinePolicy"/> outcomes, so a policy test reads as
/// "this inline resolves to this text" without repeating the single-fragment plumbing.
/// </summary>
internal static class InlinePolicyAssert
{
    public static Text AssertResolvesToText(
        IInlinePolicy policy, MarkdownInline inline, string expected) =>
        AssertText(Assert.Single(policy.Resolve(inline)), expected);
}
