using DialogueDown.Diagnostics;

namespace DialogueDown.Visualization.Lsp;

/// <summary>
/// Turns source offsets into zero-based LSP positions and ranges, wrapping the core
/// <see cref="LineMap"/> so a projection can map an AST span's offsets straight to an
/// <see cref="LspRange"/>. It composes the core map (which resolves an offset to a one-based
/// line and column) with the one-based-to-zero-based shift LSP requires, so callers never repeat
/// that conversion.
/// </summary>
internal sealed class LspLineMap
{
    private readonly LineMap _map;

    public LspLineMap(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _map = new LineMap(source);
    }

    /// <summary>The zero-based LSP position for a source <paramref name="offset"/>.</summary>
    public LspPosition Locate(int offset) => LspPosition.FromOneBased(_map.Locate(offset));

    /// <summary>
    /// The zero-based LSP range for the half-open offset span
    /// <c>[<paramref name="start"/>, <paramref name="end"/>)</c>.
    /// </summary>
    public LspRange Range(int start, int end) => new(Locate(start), Locate(end));
}
