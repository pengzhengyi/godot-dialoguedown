namespace DialogueDown.Script.Semantics;

/// <summary>
/// What a <c>Jump</c>'s target resolves to. A jump to a local anchor reaches a
/// <see cref="SceneJump"/>; a jump that names a file is a <see cref="FileScopedJump"/> left for
/// a future multi-file component; a jump with no target is an <see cref="UnresolvedJump"/>. A
/// missing local anchor is a hard error (thrown by the resolver), not a resolution state.
/// </summary>
internal abstract record JumpResolution;

/// <summary>A jump resolved to a scene in the same document.</summary>
internal sealed record SceneJump(Scene Scene) : JumpResolution;

/// <summary>
/// A jump whose target names a file, kept as its <see cref="File"/> and optional
/// <see cref="Anchor"/>. Resolving a file-scoped target — even one that names the current file
/// by path — needs cross-file support, so it is deferred rather than treated as an error.
/// </summary>
internal sealed record FileScopedJump(string File, string? Anchor) : JumpResolution;

/// <summary>A jump whose target is empty, so it points nowhere and cannot be resolved.</summary>
internal sealed record UnresolvedJump : JumpResolution;
