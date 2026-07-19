using DialogueDown.Diagnostics;

namespace DialogueDown.Cli;

/// <summary>Renders a compile's located diagnostics for the reader.</summary>
internal interface IErrataRenderer
{
    /// <summary>
    /// Writes each diagnostic in <paramref name="diagnostics"/>, sorted by position then code, as
    /// <c>file(line,column): severity CODE: message</c>, followed by a summary. Writes nothing when
    /// there are no diagnostics.
    /// </summary>
    void Render(string file, IReadOnlyList<LocatedDiagnostic> diagnostics);
}
