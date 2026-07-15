namespace DialogueDown.Configuration;

/// <summary>
/// A tag supplied by configuration: a name and, for a tag group (<c>#name=value</c> or
/// <c>##name=value</c>), an optional value. The same shape serves both a speaker's custom and
/// reserved tags; the list it sits in says whether it is reserved.
/// </summary>
public sealed record ConfiguredTag(string Name, string? Value = null);
