using DialogueDown.Configuration;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads a DialogueDown project's <c>dialogue.toml</c> into a <see cref="CompilerOptions"/> so the
/// engine-agnostic core never takes a TOML dependency. It walks Tomlyn's round-trippable syntax
/// tree, mapping each <c>[[speakers]]</c> entry to a <see cref="ConfiguredSpeaker"/> and
/// partitioning its keys into a name, an id, custom tags, and reserved tags. This type covers the
/// happy path; edge validation with located errors arrives with the configuration exception.
/// </summary>
public static class TomlConfigurationLoader
{
    private const string SpeakersTableName = "speakers";
    private const string NameKey = "name";
    private const string IdKey = "id";
    private const string TagsKey = "tags";
    private const string InlineTagValueKey = "value";
    private const string DefaultSourceName = "<dialogue.toml>";

    /// <summary>
    /// Reads and parses the <c>dialogue.toml</c> at <paramref name="path"/> into a
    /// <see cref="CompilerOptions"/>. A missing file surfaces the underlying IO exception.
    /// </summary>
    public static CompilerOptions Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Parse(File.ReadAllText(path), path);
    }

    /// <summary>
    /// Parses <paramref name="toml"/> into a <see cref="CompilerOptions"/>. A config with no
    /// <c>[[speakers]]</c> yields <see cref="CompilerOptions.Default"/>. Pass
    /// <paramref name="sourcePath"/> to name the source in diagnostics.
    /// </summary>
    public static CompilerOptions Parse(string toml, string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(toml);
        DocumentSyntax document = SyntaxParser.Parse(toml, sourcePath ?? DefaultSourceName);
        List<ConfiguredSpeaker> speakers = ReadSpeakers(document);
        return speakers.Count == 0 ? CompilerOptions.Default : new CompilerOptions { Speakers = speakers };
    }

    private static List<ConfiguredSpeaker> ReadSpeakers(DocumentSyntax document)
    {
        var speakers = new List<ConfiguredSpeaker>();
        foreach (SyntaxNode table in document.Tables)
        {
            if (table is TableArraySyntax entry && KeyName(entry.Name) == SpeakersTableName)
            {
                speakers.Add(ReadSpeaker(entry));
            }
        }

        return speakers;
    }

    private static ConfiguredSpeaker ReadSpeaker(TableArraySyntax entry)
    {
        string? name = null;
        string? id = null;
        var customTags = new List<ConfiguredTag>();
        var reservedTags = new List<ConfiguredTag>();

        foreach (KeyValueSyntax item in entry.Items)
        {
            switch (KeyName(item.Key))
            {
                case NameKey:
                    name = StringValue(item.Value);
                    break;
                case IdKey:
                    id = StringValue(item.Value);
                    break;
                case TagsKey:
                    ReadCustomTags(item.Value, customTags);
                    break;
                default:
                    AddReservedTag(KeyName(item.Key), item.Value, reservedTags);
                    break;
            }
        }

        return new ConfiguredSpeaker(name!, id, customTags, reservedTags);
    }

    private static void ReadCustomTags(SyntaxNode? value, List<ConfiguredTag> into)
    {
        var array = (ArraySyntax)value!;
        foreach (ArrayItemSyntax element in array.Items)
        {
            into.Add(element.Value is InlineTableSyntax inline
                ? ReadInlineTag(inline)
                : ParseShorthandTag(StringValue(element.Value)));
        }
    }

    private static ConfiguredTag ParseShorthandTag(string shorthand)
    {
        int separator = shorthand.IndexOf('=', StringComparison.Ordinal);
        return separator < 0
            ? new ConfiguredTag(shorthand)
            : new ConfiguredTag(shorthand[..separator], shorthand[(separator + 1)..]);
    }

    private static ConfiguredTag ReadInlineTag(InlineTableSyntax inline)
    {
        string? name = null;
        string? value = null;
        foreach (InlineTableItemSyntax field in inline.Items)
        {
            KeyValueSyntax pair = field.KeyValue!;
            string text = StringValue(pair.Value);
            if (KeyName(pair.Key) == InlineTagValueKey)
            {
                value = text;
            }
            else
            {
                name = text;
            }
        }

        return new ConfiguredTag(name!, value);
    }

    private static void AddReservedTag(string name, SyntaxNode? value, List<ConfiguredTag> into)
    {
        if (((BooleanValueSyntax)value!).Value)
        {
            into.Add(new ConfiguredTag(name));
        }
    }

    private static string KeyName(SyntaxNode? key) => key!.ToString()!.Trim();

    private static string StringValue(SyntaxNode? value) => ((StringValueSyntax)value!).Value!;
}
