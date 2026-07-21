using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Editor;

/// <summary>
/// One highlighted dialogue token: a zero-based source <see cref="Range"/> and its
/// <see cref="Kind"/> from the token legend. Projected from the Dialogue AST by
/// <see cref="SemanticTokenProjection"/> and carried in the report payload, which the editor
/// renders as a CodeMirror decoration. The same value is what a future language server publishes
/// as an LSP semantic token, so the range is the shared LSP-shaped <see cref="LspRange"/>.
/// </summary>
internal sealed record SemanticToken(LspRange Range, TokenKind Kind);
