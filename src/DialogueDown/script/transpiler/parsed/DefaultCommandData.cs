namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>The parsed form of a default command: its single free-text action.</summary>
internal sealed record DefaultCommandData(string Action) : GameCallData;
