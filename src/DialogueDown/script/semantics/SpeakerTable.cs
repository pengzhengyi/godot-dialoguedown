using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The unified lookup from a speaker's name or <c>@id</c> to its <see cref="SpeakerSymbol"/>.
/// Both maps point at the same symbols, so a name and an id for one speaker resolve to one
/// identity. <see cref="Resolve"/> maps a line's AST <see cref="Speaker"/> prefix to its
/// symbol and, for a speakerless line, to the default speaker the binder resolved — so a
/// line is never speaker-less after analysis.
/// </summary>
internal sealed class SpeakerTable
{
    private readonly IReadOnlyDictionary<string, SpeakerSymbol> _byName;
    private readonly IReadOnlyDictionary<string, SpeakerSymbol> _byId;
    private readonly SpeakerSymbol _defaultSpeaker;

    public SpeakerTable(
        IReadOnlyDictionary<string, SpeakerSymbol> byName,
        IReadOnlyDictionary<string, SpeakerSymbol> byId,
        SpeakerSymbol defaultSpeaker)
    {
        _byName = byName;
        _byId = byId;
        _defaultSpeaker = defaultSpeaker;
    }

    /// <summary>The symbol a line's speaker prefix refers to; always a symbol, never null.</summary>
    public SpeakerSymbol Resolve(Speaker speaker) => speaker switch
    {
        SpeakerDeclaration declaration => _byName[declaration.Name],
        SpeakerNameReference reference => _byName[reference.Name],
        SpeakerIdReference reference => _byId[reference.Id],
        PartialSpeakerDeclaration partial => _byId[partial.Id],
        DefaultSpeaker => _defaultSpeaker,
        _ => throw new ArgumentOutOfRangeException(
            nameof(speaker), speaker.GetType(), "Unhandled speaker kind in Resolve()."),
    };
}
