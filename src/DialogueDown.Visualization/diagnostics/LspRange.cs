namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// A half-open source range as the Language Server Protocol defines it — a <see cref="Start"/>
/// position up to (but not including) an <see cref="End"/> position, both zero-based. A zero-width
/// range (<see cref="Start"/> equals <see cref="End"/>) marks a single point, such as a synthetic
/// node's position.
/// </summary>
internal readonly record struct LspRange(LspPosition Start, LspPosition End);
