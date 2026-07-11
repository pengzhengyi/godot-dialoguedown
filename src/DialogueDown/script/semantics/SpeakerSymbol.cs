using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The resolved identity of a speaker: an optional display <see cref="Name"/>, an optional
/// stable <see cref="Id"/>, the union of <see cref="Tags"/> gathered from its declarations,
/// and whether it is the document's default speaker (<see cref="IsDefault"/>). Distinct from
/// the AST <see cref="Speaker"/> prefix: several prefixes — a declaration, a bare name, an
/// <c>@id</c> — can point at one symbol, which the speaker binder enriches in place as it
/// meets them, so an <c>@id</c> seen before its declaration is named once the declaration
/// arrives.
/// </summary>
internal sealed class SpeakerSymbol
{
    private readonly Dictionary<(string Name, string? Value), Tag> _tags = [];

    private SpeakerSymbol(string? name, string? id)
    {
        Name = name;
        Id = id;
    }

    /// <summary>The display name, or null until one is given.</summary>
    public string? Name { get; private set; }

    /// <summary>The stable id, or null if the speaker has none.</summary>
    public string? Id { get; private set; }

    /// <summary>Whether this is the document's default speaker (<c>##default</c>).</summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// The union of tags gathered from the speaker's declarations, in document order (by
    /// where each tag appears in the source).
    /// </summary>
    public IEnumerable<Tag> Tags => _tags.Values.OrderBy(tag => tag.Span.Start);

    /// <summary>A new symbol known only by <paramref name="name"/>.</summary>
    public static SpeakerSymbol ForName(string name) => new(name, id: null);

    /// <summary>A new symbol known only by <paramref name="id"/>.</summary>
    public static SpeakerSymbol ForId(string id) => new(name: null, id);

    /// <summary>Attaches a display name to a symbol first seen by its id.</summary>
    public void GiveName(string name) => Name = name;

    /// <summary>Attaches a stable id to a symbol first seen by its name.</summary>
    public void GiveId(string id) => Id = id;

    /// <summary>Marks this as the document's default speaker.</summary>
    public void MarkDefault() => IsDefault = true;

    /// <summary>Adds each tag whose (name, value) identity is not already present.</summary>
    public void MergeTags(IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
        {
            _tags.TryAdd(tag.SemanticKey(), tag);
        }
    }
}
