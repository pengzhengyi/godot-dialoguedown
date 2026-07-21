using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// Test helpers for a zero-based <see cref="LspPosition"/>: resolving it back to a document
/// offset in the source it indexes, so a range assertion can read the text it covers.
/// </summary>
internal static class LspPositionExtensions
{
    /// <summary>The zero-based document offset of this position in <paramref name="source"/>.</summary>
    public static int OffsetIn(this LspPosition position, string source)
    {
        var lines = source.Split('\n');
        var offset = 0;
        for (var line = 0; line < position.Line; line++)
        {
            offset += lines[line].Length + 1; // + the '\n'
        }

        return offset + position.Character;
    }
}
