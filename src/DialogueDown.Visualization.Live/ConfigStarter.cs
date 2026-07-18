namespace DialogueDown.Visualization.Live;

/// <summary>
/// The seed text a newly created <c>dialogue.toml</c> is written with. It is a friendly,
/// fully commented scaffold: it teaches the <c>[[speakers]]</c> schema by example, yet the
/// project keeps compiling on the built-in defaults until the reader uncomments it — so
/// creating a config has no effect on the resolved speakers until they choose to edit.
/// </summary>
internal static class ConfigStarter
{
    /// <summary>The commented starter scaffold, one worked <c>[[speakers]]</c> example.</summary>
    public const string Template =
        """
        # Speakers this project's scripts can use. Each [[speakers]] block is one speaker:
        #   name     - how the speaker is shown (required)
        #   id       - an optional short id, referenced as @id in a script
        #   tags     - optional custom tags
        #   default  - set true to make this the document's default speaker (the ##default tag)
        #
        # Uncomment the example below, or add your own. In Edit, autocompletion suggests the keys.

        # [[speakers]]
        # name = "Narrator"
        # id = "N"
        # tags = ["calm"]
        # default = true

        """;
}
