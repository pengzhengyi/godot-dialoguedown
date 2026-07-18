using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class TagValidatorTests
{
    [Fact]
    public void Validate_KnownReservedTag_ReportsNothing() =>
        Assert.Empty(Diagnostics(Reserved("default")));

    [Fact]
    public void Validate_NoTags_ReportsNothing() =>
        Assert.Empty(Diagnostics());

    [Fact]
    public void Validate_UnknownReservedTag_ReportsItAsAnError() =>
        Assert.Equal(
            DiagnosticSeverity.Error, AssertReported(Diagnostics(Reserved("bogus")), "DLG2008").Severity);

    [Fact]
    public void Validate_ReportsAtTheOffendingTagSpan()
    {
        var span = new SourceSpan(4, 6);

        Assert.Equal(span, AssertReported(Diagnostics(new ReservedTag("bogus", null, span)), "DLG2008").Span);
    }

    [Fact]
    public void Validate_ReportsEveryUnknownTag_NotJustTheFirst() =>
        Assert.Equal(2, Diagnostics(Reserved("first-bad"), Reserved("second-bad")).Count);

    // Runs the validator over the given tags and returns what it reported.
    private static IReadOnlyList<Diagnostic> Diagnostics(params ReservedTag[] tags)
    {
        var bag = new DiagnosticBag();
        TagValidator.Validate(tags, bag);
        return bag.Diagnostics;
    }

    private static ReservedTag Reserved(string name) => new(name, null, SourceSpanFactory.Span());
}
