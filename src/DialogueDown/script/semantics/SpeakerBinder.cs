using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics.Errors;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Builds the <see cref="SpeakerTable"/> from a document's speaker prefixes. It walks them in
/// document order, auto-declaring a bare name or <c>@id</c> on first use and enriching one
/// symbol in place as later prefixes add its other half or its tags, so a name and an id for
/// one speaker converge on a single identity. When the document tags no <c>##default</c>, a
/// speakerless line falls back to an anonymous, non-referable default.
/// </summary>
/// <remarks>
/// The binder is the boundary between the AST and the semantic model, so it — not the
/// error-proof <see cref="SpeakerSymbol"/> — owns the source locations diagnostics need. It
/// rejects conflicting metadata as it binds (a name bound to two ids, an id bound to two
/// names, two <c>##default</c>s, fusing two separately-used speakers) and, once every prefix
/// is bound, checks that every stable <c>@id</c> ends up named.
/// </remarks>
internal sealed class SpeakerBinder
{
    private readonly Dictionary<string, SpeakerSymbol> _speakerByName = [];
    private readonly Dictionary<string, SpeakerSymbol> _speakerById = [];
    private readonly Dictionary<SpeakerSymbol, Speaker> _originBySymbol = [];
    private SpeakerSymbol? _layerDefault;

    /// <summary>
    /// Binds a document's speaker prefixes, in document order, into a speaker table, using an
    /// anonymous default when no <c>##default</c> is tagged.
    /// </summary>
    public static SpeakerTable Bind(IEnumerable<Speaker> speakers) => Bind([], speakers);

    /// <summary>
    /// Binds two layers of speaker prefixes into one speaker table. Both layers share the
    /// name/<c>@id</c> maps, so a speaker named in both converges on one identity; each layer
    /// contributes at most one <c>##default</c>. The default speaker is chosen by precedence —
    /// the <paramref name="scriptSpeakers"/> layer's default wins over the
    /// <paramref name="configuredSpeakers"/> layer's, and an anonymous default when neither
    /// tags one.
    /// </summary>
    public static SpeakerTable Bind(
        IEnumerable<Speaker> configuredSpeakers, IEnumerable<Speaker> scriptSpeakers)
    {
        var binder = new SpeakerBinder();
        var configuredDefault = binder.BindLayer(configuredSpeakers);
        var scriptDefault = binder.BindLayer(scriptSpeakers);
        return binder.Build(scriptDefault ?? configuredDefault);
    }

    // Internal so a test can drive the per-kind dispatch directly; Bind is the entry point.
    internal void Add(Speaker speaker)
    {
        var symbol = Incorporate(speaker);
        if (symbol is not null)
        {
            // Keep the first prefix that introduced the symbol, so a later unnamed-@id error
            // can point at where an @id was first used.
            _originBySymbol.TryAdd(symbol, speaker);
        }
    }

    private static void ThrowWhenNameAndIdBelongToDifferentSpeakers(
        string name, string? id, SpeakerSymbol? byName, SpeakerSymbol? byId, SourceSpan span)
    {
        if (byName is not null && byId is not null && !ReferenceEquals(byName, byId))
        {
            throw new DialogueSemanticError(
                $"Cannot bind name '{name}' to id '@{id}': both are already in use as separate "
                + "speakers, so joining them now is ambiguous. If they are the same speaker, "
                + $"declare it (Name @{id}: …) before either is used on its own.", span);
        }
    }

    private static void ThrowWhenIdIsAlreadyBoundToAnotherName(
        string? id, string existingName, string declaredName, SourceSpan span)
    {
        if (existingName != declaredName)
        {
            throw new DialogueSemanticError(
                $"id '@{id}' is already bound to speaker '{existingName}', so it cannot also be "
                + $"bound to '{declaredName}'. Use a different id for '{declaredName}'.", span);
        }
    }

    private static void ThrowWhenNameIsAlreadyBoundToAnotherId(
        string name, string existingId, string declaredId, SourceSpan span)
    {
        if (existingId != declaredId)
        {
            throw new DialogueSemanticError(
                $"Speaker '{name}' is already bound to id '@{existingId}', so it cannot also be "
                + $"bound to id '@{declaredId}'. Give the speaker a single id.", span);
        }
    }

    private static void ThrowWhenAnotherSpeakerIsAlreadyDefault(
        SpeakerSymbol? currentDefault, SpeakerSymbol candidate, SourceSpan span)
    {
        if (currentDefault is not null && !ReferenceEquals(currentDefault, candidate))
        {
            throw new DialogueSemanticError(
                $"Two speakers are marked ##default ('{currentDefault}' and '{candidate}'); "
                + "only one default speaker is allowed.", span);
        }
    }

    private static void ThrowWhenIdWasNeverNamed(SpeakerSymbol symbol, SourceSpan span)
    {
        if (symbol.Name is null)
        {
            throw new DialogueSemanticError(
                $"Speaker '@{symbol.Id}' is used but never declared with a name. Declare it "
                + $"with a name (Name @{symbol.Id}: …) — a stable id must belong to a named "
                + "speaker.", span);
        }
    }

    private SpeakerSymbol? Incorporate(Speaker speaker)
    {
        switch (speaker)
        {
            case SpeakerDeclaration declaration:
                var declared = DeclareByNameAndId(declaration.Name, declaration.Id, declaration.Span);
                ApplyTags(declared, declaration.Tags);
                return declared;
            case SpeakerNameReference reference:
                return DeclareByName(reference.Name);
            case SpeakerIdReference reference:
                return DeclareById(reference.Id);
            case PartialSpeakerDeclaration partial:
                var byId = DeclareById(partial.Id);
                ApplyTags(byId, partial.Tags);
                return byId;
            case DefaultSpeaker:
                return null; // a speakerless line; the default is resolved when the table is built
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(speaker), speaker.GetType(), "Unhandled speaker kind in Add().");
        }
    }

    // Route each tag on a speaker prefix by kind: ##default marks the default speaker, a
    // custom tag joins the speaker's tags. Any other reserved tag is left untouched — the
    // tag validator (a later sub-pass) owns rejecting reserved tags outside the known set.
    private void ApplyTags(SpeakerSymbol symbol, IReadOnlyList<Tag> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case ReservedTag { Name: ReservedTagNames.Default } reserved:
                    MarkAsDefault(symbol, reserved.Span);
                    break;
                case CustomTag custom:
                    symbol.MergeTag(custom);
                    break;
                default:
                    break;
            }
        }
    }

    // Records this layer's default speaker. A second, different ##default within the same
    // layer is a conflict; the symbol is marked default later, once the winning layer is
    // known (see Build).
    private void MarkAsDefault(SpeakerSymbol symbol, SourceSpan span)
    {
        ThrowWhenAnotherSpeakerIsAlreadyDefault(_layerDefault, symbol, span);
        _layerDefault = symbol;
    }

    // Binds one layer of speakers into the shared maps and returns that layer's default
    // speaker, if it tagged one. A second ##default within a layer is a conflict.
    private SpeakerSymbol? BindLayer(IEnumerable<Speaker> speakers)
    {
        _layerDefault = null;
        foreach (var speaker in speakers)
        {
            Add(speaker);
        }

        return _layerDefault;
    }

    private SpeakerTable Build(SpeakerSymbol? effectiveDefault)
    {
        AssertEverySpeakerIdIsNamed();

        var defaultSpeaker = effectiveDefault ?? SpeakerSymbol.Anonymous();
        defaultSpeaker.MarkDefault();
        return new SpeakerTable(_speakerByName, _speakerById, defaultSpeaker);
    }

    // A stable @id must belong to a named speaker, so every id-keyed symbol must have ended
    // up with a name.
    private void AssertEverySpeakerIdIsNamed()
    {
        foreach (var symbol in _speakerById.Values)
        {
            ThrowWhenIdWasNeverNamed(symbol, _originBySymbol[symbol].Span);
        }
    }

    // A declaration carries a name and maybe an id; reuse whichever half is already known so
    // the two keys point at one symbol, and fill in the missing half. Conflicting metadata is
    // rejected by the guards below.
    private SpeakerSymbol DeclareByNameAndId(string name, string? id, SourceSpan span)
    {
        var byName = _speakerByName.GetValueOrDefault(name);
        var byId = id is null ? null : _speakerById.GetValueOrDefault(id);
        ThrowWhenNameAndIdBelongToDifferentSpeakers(name, id, byName, byId, span);

        var symbol = byName ?? byId ?? SpeakerSymbol.ForName(name);

        if (symbol.Name is null)
        {
            symbol.GiveName(name);
        }
        else
        {
            ThrowWhenIdIsAlreadyBoundToAnotherName(id, symbol.Name, name, span);
        }

        _speakerByName[name] = symbol;

        if (id is not null)
        {
            if (symbol.Id is null)
            {
                symbol.GiveId(id);
            }
            else
            {
                ThrowWhenNameIsAlreadyBoundToAnotherId(name, symbol.Id, id, span);
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
}
