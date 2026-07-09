using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The <c>=&gt;</c> token that marks a jump. It is only the marker: a later stage
/// pairs it with the Link that follows to assemble the composed jump, so keeping
/// them separate honors the tokenizing boundary.
/// </summary>
internal sealed record JumpIndicator(SourceSpan Span) : InlineFragment(Span);
