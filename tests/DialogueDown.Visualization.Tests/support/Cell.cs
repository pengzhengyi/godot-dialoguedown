namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// A minimal value-typed IR node: two cells with the same <see cref="Name"/> are
/// value-equal, which lets a test prove the walk keys on <em>reference</em>
/// identity rather than value equality.
/// </summary>
internal sealed record Cell(string Name);
