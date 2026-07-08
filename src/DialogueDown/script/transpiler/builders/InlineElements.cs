namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The inline fragment kinds a context allows, so one inline walk serves speech and
/// labels alike. <see cref="All"/> permits everything; <see cref="StylingOnly"/>
/// permits only text, tags, and styling — the functional image, link, jump, and game
/// call are excluded, as they carry no meaning inside a label. Text, tags, and styling
/// are allowed everywhere.
/// </summary>
[Flags]
internal enum InlineElements
{
    None = 0,
    Text = 1 << 0,
    Tag = 1 << 1,
    Styling = 1 << 2,
    Image = 1 << 3,
    Link = 1 << 4,
    GameCall = 1 << 5,
    Jump = 1 << 6,
    LineBreak = 1 << 7,

    StylingOnly = Text | Tag | Styling,
    All = StylingOnly | Image | Link | GameCall | Jump | LineBreak,
}
