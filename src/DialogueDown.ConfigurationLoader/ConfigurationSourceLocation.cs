namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Where a configuration problem sits: the source name and a one-based line and column, so a
/// message or tool can point at the offending characters. It reads like an editor location and
/// formats as <c>source(line,column)</c>.
/// </summary>
public readonly record struct ConfigurationSourceLocation(string Source, int Line, int Column)
{
    /// <summary>Formats the location as <c>source(line,column)</c>, matching common tool output.</summary>
    public override string ToString() => $"{Source}({Line},{Column})";
}
