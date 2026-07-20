namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// A zero-based position in the source — a <see cref="Line"/> and a <see cref="Character"/>
/// offset within that line, counted in UTF-16 code units. It is the Language Server Protocol
/// counterpart of the core's one-based <see cref="DialogueDown.Diagnostics.LinePosition"/>, which
/// <see cref="DiagnosticProjection"/> decrements into this shape.
/// </summary>
internal readonly record struct LspPosition(int Line, int Character);
