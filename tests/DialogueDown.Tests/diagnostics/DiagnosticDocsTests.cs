using DialogueDown.Compilation;
using DialogueDown.Configuration;

namespace DialogueDown.Tests.Diagnostics;

// The core promise of the reader-facing docs: every documented example is checked against the real
// compiler, so the "Error codes" page can never show an example that does not behave as claimed.
public sealed class DiagnosticDocsTests
{
    public static TheoryData<string> CodesWithExamples()
    {
        var codes = new TheoryData<string>();
        foreach (var doc in DiagnosticDocs.All.Where(doc => doc.Example is not null))
        {
            codes.Add(doc.Descriptor.Code);
        }

        return codes;
    }

    [Fact]
    public void EveryCatalogCodeIsDocumented()
    {
        var documented = DiagnosticDocs.All.Select(doc => doc.Descriptor.Code).ToHashSet();
        var catalog = DiagnosticCatalogReflection.Descriptors().Select(descriptor => descriptor.Code);

        // Every diagnostic the compiler can report needs a reader-facing entry, so the Error codes
        // page is complete. Adding a code to the catalog without documenting it fails here.
        Assert.All(catalog, code => Assert.Contains(code, documented));
    }

    [Fact]
    public void OnlyTheKnownUnproducibleCodesLackAnExample()
    {
        var withoutExample = DiagnosticDocs.All
            .Where(doc => doc.Example is null)
            .Select(doc => doc.Descriptor.Code)
            .ToHashSet();

        // A missing example must be a deliberate choice (the code's producer has not landed), not an
        // oversight — so the set of example-less codes must match the documented allowlist exactly.
        Assert.Equal(DiagnosticDocs.WithoutExampleYet, withoutExample);
    }

    [Theory]
    [MemberData(nameof(CodesWithExamples))]
    public void TheTriggeringExampleReportsItsCode(string code)
    {
        var example = DiagnosticDocs.ByCode[code].Example!;

        Assert.Contains(code, ReportedCodes(example.Broken));
    }

    [Theory]
    [MemberData(nameof(CodesWithExamples))]
    public void TheFixedExampleNoLongerReportsTheCode(string code)
    {
        var example = DiagnosticDocs.ByCode[code].Example!;

        Assert.DoesNotContain(code, ReportedCodes(example.Fixed));
    }

    [Theory]
    [MemberData(nameof(CodesWithExamples))]
    public void EveryHighlightOccursInItsExample(string code)
    {
        var example = DiagnosticDocs.ByCode[code].Example!;

        Assert.All(example.BrokenHighlights, highlight => Assert.Contains(highlight, example.Broken));
        Assert.All(example.FixedHighlights, highlight => Assert.Contains(highlight, example.Fixed));
    }

    // Best-effort so every stage runs and reports, even when an earlier error would otherwise halt
    // the compile — the page documents each code in isolation.
    private static IReadOnlyList<string> ReportedCodes(string source)
    {
        var compiler = ScriptCompilerFactory.CreateDefault(
            CompilerOptions.Default with { Mode = CompilationMode.BestEffort });
        return compiler.Compile(source).LocatedDiagnostics.Select(diagnostic => diagnostic.Code).ToList();
    }
}
