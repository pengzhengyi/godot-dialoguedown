using System.Text.RegularExpressions;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Turns heading text into a GitHub-style anchor slug, matching the
/// <a href="https://github.com/Flet/github-slugger"><c>github-slugger</c></a> algorithm an
/// editor's GitHub-flavored Markdown tooling uses. Parity matters: a writer who autocompletes
/// a jump target against a scene heading gets the editor's slug, so the compiler must produce
/// the identical one or the jump silently breaks. The rules are therefore reproduced exactly —
/// lowercase, drop the same punctuation set (keeping letters, digits, underscores, and
/// existing hyphens), and turn each space into a hyphen, with no trimming or run collapsing.
/// </summary>
internal static class Slug
{
    // The exact character set github-slugger removes: general and supplemental punctuation
    // blocks, common ASCII punctuation, and the curly apostrophe — but not spaces, hyphens,
    // or underscores, which the slug keeps or turns into hyphens.
    private static readonly Regex _strippedPunctuation = new(
        "[\u2000-\u206F\u2E00-\u2E7F\\\\'!\"#$%&()*+,./:;<=>?@\\[\\]^`{|}~\u2019]",
        RegexOptions.Compiled);

    /// <summary>The slug for <paramref name="text"/>; empty when nothing sluggable remains.</summary>
    public static string From(string text) =>
        _strippedPunctuation.Replace(text.ToLowerInvariant(), string.Empty).Replace(' ', '-');
}
