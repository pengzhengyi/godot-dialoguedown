using System.Runtime.CompilerServices;

namespace DialogueDown.Tests.Diagnostics;

/// <summary>
/// Guards that the committed <c>docs/guide/error-codes.md</c> is the exact rendering of the
/// diagnostic catalog, so the published reference never drifts from the code. When the catalog
/// changes, regenerate the page with
/// <c>DIALOGUEDOWN_UPDATE_DOCS=1 dotnet test --filter DiagnosticCatalogDocumentation</c>.
/// </summary>
public sealed class DiagnosticCatalogDocumentationTests
{
    private const string PagePath = "docs/guide/error-codes.md";
    private const string UpdateVariable = "DIALOGUEDOWN_UPDATE_DOCS";

    private const string RegenerateHint =
        UpdateVariable + "=1 dotnet test --filter DiagnosticCatalogDocumentation";

    [Fact]
    public void ErrorCodesPageStaysInSyncWithTheCatalog()
    {
        var path = Path.Combine(RepositoryRoot(), "docs", "guide", "error-codes.md");
        var rendered = DiagnosticCatalogMarkdown.Render();

        if (ShouldUpdate())
        {
            File.WriteAllText(path, rendered);
            return;
        }

        Assert.True(File.Exists(path), $"{PagePath} is missing; regenerate it with `{RegenerateHint}`.");
        Assert.True(
            Normalize(File.ReadAllText(path)) == Normalize(rendered),
            $"{PagePath} is out of sync with the diagnostic catalog. Regenerate it with `{RegenerateHint}`.");
    }

    private static bool ShouldUpdate() =>
        Environment.GetEnvironmentVariable(UpdateVariable) is "1" or "true";

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    // Walks up from this test's source location to the checkout root, identified by its solution
    // file, so the committed page is found wherever the runner places the compiled binaries.
    private static string RepositoryRoot([CallerFilePath] string callerPath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(callerPath)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DialogueDown.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find the repository root (DialogueDown.sln).");
    }
}
