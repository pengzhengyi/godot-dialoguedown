using DialogueDown.Configuration;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads the <c>[[speakers]]</c> entries of a parsed <see cref="DocumentSyntax"/> into
/// <see cref="ConfiguredSpeaker"/>s, in document order, validating each as it goes. It owns the
/// mapping from TOML shape to the configuration model — a name, an optional id, custom tags (DSL
/// shorthand strings or an inline-table escape hatch), and reserved typed keys — and rejects a
/// malformed speaker with a located <see cref="DialogueConfigurationException"/>. It maps syntax to
/// data generically; interpreting a reserved tag's meaning stays downstream in the compiler.
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
        string? firstDefaultName = null;

        foreach (var entry in SpeakerEntries(document))
        {
            var speaker = ReadSpeaker(entry);
            if (IsDefault(speaker))
            {
                RejectSecondDefault(entry, speaker, ref firstDefaultName);
            }

            speakers.Add(speaker);
        }

        return speakers;
    }

    private static IEnumerable<TableArraySyntax> SpeakerEntries(DocumentSyntax document) =>
        document.Tables
            .OfType<TableArraySyntax>()
            .Where(entry => KeyName(entry.Name) == SpeakersTableName);

    private static ConfiguredSpeaker ReadSpeaker(TableArraySyntax entry)
    {
        string? name = null;
        string? id = null;
        var customTags = new List<ConfiguredTag>();
        var reservedTags = new List<ConfiguredTag>();

        foreach (var item in entry.Items)
        {
            switch (KeyName(item.Key))
            {
                case NameKey:
                    name = RequireNonEmptyString(item);
                    break;
                case IdKey:
                    id = RequireNonEmptyString(item);
                    break;
                case TagsKey:
                    ReadCustomTags(item, customTags);
                    break;
                default:
                    AddReservedTag(item, reservedTags);
                    break;
            }
        }

        return new ConfiguredSpeaker(RequireName(name, entry), id, customTags, reservedTags);
    }

    private static void ReadCustomTags(KeyValueSyntax item, List<ConfiguredTag> into)
    {
        if (item.Value is not ArraySyntax array)
        {
            throw Error("'tags' must be an array of tag strings or inline tables.", item.Value!);
        }

        foreach (var element in array.Items)
        {
            into.Add(ReadCustomTag(element.Value!));
        }
    }

    private static ConfiguredTag ReadCustomTag(SyntaxNode value) => value switch
    {
        InlineTableSyntax inline => ReadInlineTag(inline),
        StringValueSyntax shorthand => ParseShorthandTag(shorthand.Value!),
        _ => throw Error("A tag must be a string or an inline table with a 'name'.", value),
    };

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
        foreach (var field in inline.Items)
        {
            var pair = field.KeyValue!;
            switch (KeyName(pair.Key))
            {
                case NameKey:
                    name = RequireString(pair);
                    break;
                case InlineTagValueKey:
                    value = RequireString(pair);
                    break;
                default:
                    throw Error(
                        $"An inline-table tag has only 'name' and 'value'; "
                        + $"'{KeyName(pair.Key)}' is not allowed.", pair.Value!);
            }
        }

        if (name is null)
        {
            throw Error("An inline-table tag must have a 'name'.", inline);
        }

        return new ConfiguredTag(name, value);
    }

    private static void AddReservedTag(KeyValueSyntax item, List<ConfiguredTag> into)
    {
        var name = KeyName(item.Key);
        if (!ReservedTagNames.Known.Contains(name))
        {
            throw Error(
                $"Unknown speaker key '{name}'. Use 'name', 'id', 'tags', or a known reserved tag.",
                item.Key!);
        }

        switch (item.Value)
        {
            case BooleanValueSyntax { Value: true }:
                into.Add(new ConfiguredTag(name));
                break;
            case BooleanValueSyntax { Value: false }:
                break;
            case StringValueSyntax valued:
                into.Add(new ConfiguredTag(name, valued.Value!));
                break;
            default:
                throw Error($"Reserved tag '{name}' must be a boolean or a string.", item.Value!);
        }
    }

    private static void RejectSecondDefault(
        TableArraySyntax entry, ConfiguredSpeaker speaker, ref string? firstDefaultName)
    {
        if (firstDefaultName is not null)
        {
            throw Error(
                $"Two speakers are marked default ('{firstDefaultName}' and '{speaker.Name}'); "
                + "only one default speaker is allowed.", entry);
        }

        firstDefaultName = speaker.Name;
    }

    private static bool IsDefault(ConfiguredSpeaker speaker) =>
        speaker.ReservedTags.Any(tag => tag.Name == ReservedTagNames.Default);

    private static string RequireName(string? name, TableArraySyntax entry) =>
        name ?? throw Error("A speaker must have a 'name'.", entry);

    private static string RequireNonEmptyString(KeyValueSyntax item)
    {
        var value = RequireString(item);
        return value.Length > 0
            ? value
            : throw Error($"'{KeyName(item.Key)}' must not be empty.", item.Value!);
    }

    private static string RequireString(KeyValueSyntax item) => item.Value is StringValueSyntax text
        ? text.Value!
        : throw Error($"'{KeyName(item.Key)}' must be a string.", item.Value!);

    private static DialogueConfigurationException Error(string message, SyntaxNode node) =>
        new(message, TomlLocation.From(node.Span));

    private static string KeyName(KeySyntax? key)
    {
        var resolved = key!;

        // A dotted key (a.b) has no single flat name; keep its full text so it never matches a
        // structural key and is rejected, rather than being read as only its first segment.
        if (resolved.DotKeys is { ChildrenCount: > 0 })
        {
            return resolved.ToString()!.Trim();
        }

        // Resolve the key's semantic name so a quoted key equals its bare form, per TOML: "name"
        // and name are the same key.
        return resolved.Key is BareKeySyntax bare
            ? bare.Key!.Text!
            : ((StringValueSyntax)resolved.Key!).Value!;
    }
}
