using DialogueDown.Script.Semantics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for speaker symbols, so a test can state the tags it expects — by name
/// and value, in document order — instead of projecting and comparing them by hand.
/// </summary>
internal static class SpeakerSymbolAssert
{
    public static void AssertTags(SpeakerSymbol symbol, params (string Name, string? Value)[] expected) =>
        Assert.Equal(expected, symbol.Tags.Select(tag => (tag.Name, tag.Value)));
}
