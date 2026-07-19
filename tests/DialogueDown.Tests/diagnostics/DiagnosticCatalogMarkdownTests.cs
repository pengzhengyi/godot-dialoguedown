using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogMarkdownTests
{
    private static readonly string _rendered = DiagnosticCatalogMarkdown.Render();

    [Fact]
    public void OpensWithASingleErrorCodesTitle()
    {
        Assert.StartsWith("# Error codes\n", _rendered);

        var topLevelHeadings = _rendered
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
            Assert.Contains($"**{descriptor.Title}** · {descriptor.DefaultSeverity}", _rendered);
            Assert.Contains(descriptor.MessageFormat, _rendered);
        }
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
}
