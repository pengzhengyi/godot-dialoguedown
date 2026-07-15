using DialogueDown.Configuration;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads the <c>[[speakers]]</c> entries of a parsed <see cref="DocumentSyntax"/> into
/// <see cref="ConfiguredSpeaker"/>s, in document order. It owns the mapping from TOML shape to the
/// configuration model: a name, an optional id, custom tags (DSL shorthand strings or an
/// inline-table escape hatch), and reserved typed keys. It is the future home for speaker-level
/// edge validation.
/// </summary>
internal sealed class ConfiguredSpeakerReader
{
    private const string SpeakersTableName = "speakers";
    private const string NameKey = "name";
    private const string IdKey = "id";
    private const string TagsKey = "tags";
    private const string InlineTagValueKey = "value";

    public IReadOnlyList<ConfiguredSpeaker> Read(DocumentSyntax document)
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
