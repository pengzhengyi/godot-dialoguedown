using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Builds the <see cref="SpeakerTable"/> from a document's speaker prefixes. It walks them in
/// document order, auto-declaring a bare name or <c>@id</c> on first use and enriching one
/// symbol in place as later prefixes add its other half or its tags, so a name and an id for
/// one speaker converge on a single identity. When the document tags no <c>##default</c>, a
/// speakerless line falls back to an anonymous, non-referable default.
/// </summary>
internal sealed class SpeakerBinder
{
    private const string DefaultSpeakerTagName = "default";

    private readonly Dictionary<string, SpeakerSymbol> _speakerByName = [];
    private readonly Dictionary<string, SpeakerSymbol> _speakerById = [];

    /// <summary>Binds the given speaker prefixes, in document order, into a speaker table.</summary>
    public static SpeakerTable Bind(IEnumerable<Speaker> speakers)
    {
        var binder = new SpeakerBinder();
        foreach (var speaker in speakers)
        {
            binder.Add(speaker);
        }

        return binder.Build();
    }

    // Internal so a test can drive the per-kind dispatch directly; Bind is the entry point.
    internal void Add(Speaker speaker)
    {
        switch (speaker)
        {
            case SpeakerDeclaration declaration:
                ApplyTags(DeclareByNameAndId(declaration.Name, declaration.Id), declaration.Tags);
                break;
            case SpeakerNameReference reference:
                DeclareByName(reference.Name);
                break;
            case SpeakerIdReference reference:
                DeclareById(reference.Id);
                break;
            case PartialSpeakerDeclaration partial:
                ApplyTags(DeclareById(partial.Id), partial.Tags);
                break;
            case DefaultSpeaker:
                break; // a speakerless line; the default is resolved when the table is built
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(speaker), speaker.GetType(), "Unhandled speaker kind in Add().");
        }
    }

    // Route each tag on a speaker prefix by kind: ##default marks the default speaker, a
    // custom tag joins the speaker's tags. Any other reserved tag is left untouched — the
    // tag validator (a later sub-pass) owns rejecting reserved tags outside the known set.
    private static void ApplyTags(SpeakerSymbol symbol, IReadOnlyList<Tag> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case ReservedTag { Name: DefaultSpeakerTagName }:
                    symbol.MarkDefault();
                    break;
                case CustomTag custom:
                    symbol.MergeTag(custom);
                    break;
                default:
                    break;
            }
        }
    }

    private static SpeakerSymbol MakeAnonymousDefault()
    {
        var anonymous = SpeakerSymbol.Anonymous();
        anonymous.MarkDefault();
        return anonymous;
    }

    private SpeakerTable Build()
    {
        var declaredDefault = _speakerByName.Values.FirstOrDefault(symbol => symbol.IsDefault);
        return new SpeakerTable(
            _speakerByName, _speakerById, declaredDefault ?? MakeAnonymousDefault());
    }

    // A declaration carries a name and maybe an id; reuse whichever half is already known so
    // the two keys point at one symbol, and fill in the missing half.
    private SpeakerSymbol DeclareByNameAndId(string name, string? id)
    {
        var symbol = GetExistingSpeaker(name, id) ?? SpeakerSymbol.ForName(name);
        if (symbol.Name is null)
        {
            symbol.GiveName(name);
        }

        _speakerByName[name] = symbol;

        if (id is not null)
        {
            if (symbol.Id is null)
            {
                symbol.GiveId(id);
            }

            _speakerById[id] = symbol;
        }

        return symbol;
    }

    private SpeakerSymbol DeclareByName(string name)
    {
        if (!_speakerByName.TryGetValue(name, out var symbol))
        {
            symbol = SpeakerSymbol.ForName(name);
            _speakerByName[name] = symbol;
        }

        return symbol;
    }

    private SpeakerSymbol DeclareById(string id)
    {
        if (!_speakerById.TryGetValue(id, out var symbol))
        {
            symbol = SpeakerSymbol.ForId(id);
            _speakerById[id] = symbol;
        }

        return symbol;
    }

    private SpeakerSymbol? GetExistingSpeaker(string name, string? id)
    {
        if (_speakerByName.TryGetValue(name, out var speakerByName))
        {
            return speakerByName;
        }

        if (id is not null && _speakerById.TryGetValue(id, out var speakerById))
        {
            return speakerById;
        }

        return null;
    }
}
