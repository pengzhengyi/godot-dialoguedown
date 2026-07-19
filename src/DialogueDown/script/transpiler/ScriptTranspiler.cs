using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// The default <see cref="IScriptTranspiler"/>: it walks the Markdown block tree with the
/// <see cref="BlockBuilder"/> and wraps the result in a <see cref="ScriptDocument"/>.
/// </summary>
internal sealed class ScriptTranspiler(BlockBuilder blockBuilder) : IScriptTranspiler
{
    public ScriptDocument Transpile(MarkdownDocument document, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        // TODO(diagnostics): the inline builders (game calls, disallowed labels) still throw; they
        // will report into context.Diagnostics when their producers land. The speaker prefix
        // already reports here through the block builder.
        return new ScriptDocument(blockBuilder.Build(document.Blocks, context.Diagnostics));
    }
}
