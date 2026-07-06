using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// The result of recognizing a speaker prefix on a line: the parsed
/// <see cref="Speaker"/> and the offset (within the leading text) where speech
/// begins, just after the colon.
/// </summary>
internal sealed record SpeakerPrefix(Speaker Speaker, int SpeechStart);
