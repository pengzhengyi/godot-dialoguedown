using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogMarkdownTests
{
    private static readonly string _rendered = DiagnosticCatalogMarkdown.Render();

    [Fact]
    public void OpensWithASingleErrorCodesTitle()
    {
        Assert.StartsWith("# Error codes\n", _rendered);

        // Example scripts contain their own "# Scene" heading lines inside <pre> blocks; those are
        // literal script text, not document headings, so exclude them before counting the H1.
        var topLevelHeadings = WithoutPreBlocks(_rendered)
            .Split('\n')
            .Count(line => line.StartsWith("# ", StringComparison.Ordinal));
        Assert.Equal(1, topLevelHeadings);
    }

    [Fact]
    public void RendersEveryCatalogCodeAsAnAnchoredSubsection()
    {
        foreach (var descriptor in DiagnosticCatalogReflection.Descriptors())
        {
            // A bare code heading gives a stable slug (### DLG2001 -> #dlg2001) for deep links.
            Assert.Contains($"### {descriptor.Code}\n", _rendered);
        }
    }

    [Fact]
    public void ShowsEachDescriptorsTitleSeverityAndMessage()
    {
        foreach (var descriptor in DiagnosticCatalogReflection.Descriptors())
        {
            Assert.Contains($">{descriptor.DefaultSeverity}</span> · {descriptor.Title}", _rendered);
            Assert.Contains(descriptor.MessageFormat, _rendered);
        }
    }

    [Fact]
    public void MarksTheChangedTokensInADocumentedExample()
    {
        // DLG1101 marks the offending tags in red in the broken script and the added speaker name
        // in green in the fix, so the change stands out in both examples.
        Assert.Contains("<mark class=\"dd-mark-bad\">#excited</mark>", _rendered);
        Assert.Contains("<mark class=\"dd-mark-fix\">Alice </mark>#excited", _rendered);
        Assert.Contains("<span class=\"dd-eg-fix\">Fix</span>", _rendered);
        Assert.Contains("<span class=\"dd-eg-bad\">Triggering example</span>", _rendered);
    }

    [Fact]
    public void GroupsCodesUnderTheirCategoryHeadingInReportingOrder()
    {
        var syntaxHeading = _rendered.IndexOf("## Syntax (`DLG1xxx`)", StringComparison.Ordinal);
        var semanticHeading = _rendered.IndexOf("## Semantic (`DLG2xxx`)", StringComparison.Ordinal);

        Assert.True(syntaxHeading >= 0, "the Syntax section is present");
        Assert.True(semanticHeading > syntaxHeading, "Semantic follows Syntax");
        Assert.True(_rendered.IndexOf("### DLG1003", StringComparison.Ordinal) < semanticHeading);
        Assert.True(_rendered.IndexOf("### DLG2001", StringComparison.Ordinal) > semanticHeading);
    }

    [Fact]
    public void OrdersCodesWithinACategoryAscending()
    {
        var semanticCodes = DiagnosticCatalogReflection.Descriptors()
            .Where(descriptor => descriptor.Category == DiagnosticCategory.Semantic)
            .Select(descriptor => descriptor.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        var positions = semanticCodes
            .Select(code => _rendered.IndexOf($"### {code}\n", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.OrderBy(position => position).ToList(), positions);
    }

    [Fact]
    public void OmitsACategoryThatHasNoCodesYet()
    {
        // Style (DLG3xxx) is a defined range with no descriptors, so it renders no section.
        Assert.DoesNotContain("## Style (`DLG3xxx`)", _rendered);
    }

    [Fact]
    public void IsDeterministic()
    {
        Assert.Equal(_rendered, DiagnosticCatalogMarkdown.Render());
    }

    private static string WithoutPreBlocks(string markdown) =>
        System.Text.RegularExpressions.Regex.Replace(
            markdown, "<pre[^>]*>.*?</pre>", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
}
