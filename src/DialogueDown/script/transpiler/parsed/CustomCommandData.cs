namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>The parsed form of a named command: its name and its arguments.</summary>
internal sealed record CustomCommandData(string Name, IReadOnlyList<string> Args) : GameCallData;
