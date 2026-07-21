namespace DialogueDown.Visualization.Live;

/// <summary>
/// How a <see cref="LiveSession.CreateConfig">create-config</see> request settled: the file was
/// freshly written from the starter template, an existing starter-template file was adopted
/// idempotently (a create retry after a lost response), or a differing file already exists and the
/// create is a conflict.
/// </summary>
internal enum CreateConfigStatus
{
    /// <summary>The starter <c>dialogue.toml</c> was created and adopted.</summary>
    Created,

    /// <summary>An existing file equal to the starter template was adopted without rewriting.</summary>
    Adopted,

    /// <summary>A differing <c>dialogue.toml</c> already exists; nothing was written.</summary>
    Conflict,
}

/// <summary>
/// The outcome of a create-config request: its <see cref="Status"/> and a <see cref="Payload"/> —
/// the recompiled document JSON for <see cref="CreateConfigStatus.Created"/> and
/// <see cref="CreateConfigStatus.Adopted"/>, or a reader-facing message for
/// <see cref="CreateConfigStatus.Conflict"/>.
/// </summary>
internal sealed record CreateConfigResult(CreateConfigStatus Status, string Payload);
