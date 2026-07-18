namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// A configured speaker's tag as shown in the Config tab: its <see cref="Name"/>, an optional
/// <see cref="Value"/>, and whether it is <see cref="Reserved"/> (a reserved name such as
/// <c>default</c>) rather than custom — the flag the client colors chips by.
/// </summary>
internal sealed record ConfiguredTagView(string Name, string? Value, bool Reserved);
