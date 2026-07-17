using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Object Mother for <see cref="DiagnosticsContext"/>, so a stage call in a test threads a
/// context through one place with sane defaults (an empty source and a fresh
/// <see cref="DiagnosticBag"/>) and the stage signature change touches only this file.
/// </summary>
internal static class DiagnosticsContextFactory
{
    public static DiagnosticsContext Context(string source = "", IDiagnosticSink? diagnostics = null) =>
        new(source, diagnostics ?? new DiagnosticBag());
}
