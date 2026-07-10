using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// The desugarer rewrite: a <see cref="DialogueAstRewriter"/> wired with the local
/// normalization rules. It assembles jumps in every fragment sequence and fills the
/// default speaker on every speaker-less line; everything else clones through unchanged.
/// </summary>
internal sealed class Desugarer : DialogueAstRewriter
{
    // Each override calls base first, so the tree is rewritten bottom-up before a rule
    // runs: base.RewriteLine rewrites the speech (assembling any jumps in it) and the
    // speaker, and base.RewriteFragments rewrites nested fragments (e.g. a link label);
    // only then does the rule transform the already-rewritten node.
    protected override Line RewriteLine(Line line) =>
        DefaultSpeakerFiller.Fill(base.RewriteLine(line));

    protected override IReadOnlyList<InlineFragment> RewriteFragments(
        IReadOnlyList<InlineFragment> fragments) =>
        JumpAssembler.Assemble(base.RewriteFragments(fragments));
}
