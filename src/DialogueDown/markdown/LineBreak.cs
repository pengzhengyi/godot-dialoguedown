using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// A line break inside a paragraph. <see cref="IsHard"/> tells a hard break (two
/// trailing spaces or a trailing backslash) apart from a soft break (a plain
/// newline). This layer only records the break faithfully; the dialogue compiler
/// decides that a hard break starts a new speech while a soft break is a
/// space-joined continuation of the same one.
/// </summary>
internal sealed record LineBreak(bool IsHard, SourceSpan Span) : MarkdownInline(Span);
