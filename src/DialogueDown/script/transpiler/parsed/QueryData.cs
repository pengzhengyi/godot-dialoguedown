namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>The parsed form of a query: the key it reads.</summary>
internal sealed record QueryData(string Key) : GameCallData;
