using DialogueDown.Common;
using DialogueDown.Common.Errors;

namespace DialogueDown.Script.Semantics.Errors;

/// <summary>
/// A semantic error in the dialogue DSL — for example a speaker whose <c>@id</c> is
/// never given a name, two speakers that both claim <c>##default</c>, or a jump to a
/// scene anchor that does not exist. The span names the offending text so the message
/// can point straight at it.
/// </summary>
internal sealed class DialogueSemanticError : SemanticError
{
    public DialogueSemanticError(string message, SourceSpan span)
        : base(message, span)
    {
    }
}
