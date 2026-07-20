using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests;

/// <summary>
/// Shared harness for the configuration readers' tests: it parses a TOML snippet under a fixed
/// source name and runs a reader over the result, so each reader's tests stay one-liners and a new
/// reader reuses the same setup. The reader delegate is any reader's <c>Read</c> method.
/// </summary>
internal static class TomlConfigReading
{
    /// <summary>The source name every snippet parses under — also the source of a located error.</summary>
    public const string SourceName = "dialogue.toml";

    public static T Read<T>(string toml, Func<DocumentSyntax, T> reader)
    {
        var document = new TomlDocumentParser(SourceName).Parse(toml);
        return reader(document);
    }

    public static DialogueConfigurationException Reject<T>(string toml, Func<DocumentSyntax, T> reader) =>
        Assert.Throws<DialogueConfigurationException>(() => Read(toml, reader));
}
