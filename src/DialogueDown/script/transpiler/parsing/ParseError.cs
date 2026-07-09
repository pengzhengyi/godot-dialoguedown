namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Why a parse failed. Filled only on the failure path — typically the underlying
/// grammar's rendered message — and surfaced as the inner detail of a raised
/// error, separate from the author-facing message.
/// </summary>
internal readonly record struct ParseError(string Detail);
