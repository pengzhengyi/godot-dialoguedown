using DialogueDown.Configuration;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics.Errors;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Checks each reserved (<c>##name</c>) tag against DialogueDown's <see cref="ReservedTagNames.Known"/>
/// set and rejects one whose name is not recognized. Custom (<c>#name</c>) tags are opaque and
/// never reach here; the transpiler already guarantees a tag rides on a speaker, image, or
/// speech, so a tag with nothing to attach to is not re-checked.
/// </summary>
internal static class TagValidator
{
    /// <summary>Validates every reserved tag in <paramref name="tags"/>, throwing on an unknown one.</summary>
    public static void Validate(IEnumerable<ReservedTag> tags)
    {
        foreach (var tag in tags)
        {
            if (!ReservedTagNames.Known.Contains(tag.Name))
            {
                throw new DialogueSemanticError(
                    $"'##{tag.Name}' is not a known reserved tag. Use a custom tag ('#{tag.Name}') "
                    + "or one of DialogueDown's reserved tags.", tag.Span);
            }
        }
    }
}
