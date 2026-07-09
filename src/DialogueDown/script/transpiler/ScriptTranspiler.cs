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
    public ScriptDocument Transpile(MarkdownDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);

        // TODO(diagnostics): source is validated but not yet read — the transpile works
        // off the text and spans already in the Markdown AST. Thread source into error
        // reporting when the diagnostics phase lands, so a message can quote the offending
        // text at a node's span.
        return new ScriptDocument(blockBuilder.Build(document.Blocks));
    }
}
