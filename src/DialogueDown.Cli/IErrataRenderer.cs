using DialogueDown.Diagnostics;

namespace DialogueDown.Cli;

/// <summary>Renders a compile's located diagnostics for the reader.</summary>
internal interface IErrataRenderer
{
    /// <summary>
    /// Writes each diagnostic in <paramref name="diagnostics"/>, sorted by position then code,
    /// followed by a summary. On an interactive console it renders rich Errata blocks with a source
    /// snippet and caret over <paramref name="source"/>; otherwise it writes the greppable
    /// <c>file(line,column): severity CODE: message</c> one-liner. Writes nothing when there are no
    /// diagnostics.
    /// </summary>
    void Render(string file, string source, IReadOnlyList<LocatedDiagnostic> diagnostics);
}
