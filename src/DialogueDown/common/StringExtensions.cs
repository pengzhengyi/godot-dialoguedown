namespace DialogueDown.Common;

/// <summary>
/// Small, general-purpose helpers over <see cref="string"/> shared across the library.
/// </summary>
internal static class StringExtensions
{
    /// <summary>The string itself, or null when it is null or empty.</summary>
    public static string? NullIfEmpty(this string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
