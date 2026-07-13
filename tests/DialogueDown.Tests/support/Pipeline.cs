using DialogueDown.Script.Desugar;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Runs the real compiler stages — parse, transpile, desugar — over source, so an
/// integration test can feed a downstream stage a genuine desugared tree instead of a
/// hand-built one.
/// </summary>
internal static class Pipeline
{
    public static DesugaredScriptDocument UntilDesugared(string source)
    {
        var markdown = MarkdownParserFactory.MarkdownParser().Parse(source);
        var script = TranspilerBuilderFactory.ScriptTranspiler().Transpile(markdown, source);
        return DesugarerFactory.ScriptDesugarer().Desugar(script, source);
    }
}
