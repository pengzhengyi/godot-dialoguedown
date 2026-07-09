using DialogueDown.Common;
using DialogueDown.Common.Errors;

namespace DialogueDown.Script.Transpiler.Errors;

/// <summary>
/// A syntax error in the dialogue DSL — for example a code span that is neither a
/// valid query nor command, or a malformed tag. The span names the offending text
/// so the message can point straight at it.
/// </summary>
internal sealed class DialogueSyntaxError : SyntaxError
{
    public DialogueSyntaxError(string message, SourceSpan span)
        : base(message, span)
    {
    }
}
