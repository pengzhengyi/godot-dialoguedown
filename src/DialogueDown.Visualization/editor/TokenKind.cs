namespace DialogueDown.Visualization.Editor;

/// <summary>
/// The vocabulary of dialogue tokens the editor highlights — the semantic-tokens "legend". Each
/// value names a dialogue-specific construct that generic Markdown highlighting does not
/// understand; a future language server publishes this set as its semantic-tokens legend. The
/// members serialize by name (PascalCase) into the report payload, matching the other payload
/// enums.
/// </summary>
internal enum TokenKind
{
    /// <summary>
    /// A speaker prefix — the display name and its optional <c>@id</c> together
    /// (<c>Alice</c>, <c>@alice</c>, or <c>Alice @alice</c>). A coarse token; splitting the name,
    /// the id, and the separator apart is deferred (see the design note).
    /// </summary>
    Speaker,

    /// <summary>A custom tag, including the <c>#</c> and any <c>=value</c> (<c>#happy</c>).</summary>
    CustomTag,

    /// <summary>A reserved tag, including the <c>##</c> (<c>##narrator</c>).</summary>
    ReservedTag,

    /// <summary>The <c>=&gt;</c> that marks a jump.</summary>
    JumpIndicator,
}
