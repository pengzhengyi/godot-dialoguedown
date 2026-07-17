# Guide

Writer-facing documentation for **authoring dialogue** with DialogueDown. If you
want to *write* branching dialogue — not modify the compiler — start here.

## In this section

- **[Overview](overview.md)** — the architecture at a glance, the three
  representations a script passes through (source → compiled model → runtime
  graph), and the current implementation status.
- **[Script language specification](script-language.md)** — the writer-facing
  dialogue syntax: speakers, speech, choices, jumps, tags, game calls, and inline
  styling, with examples.
- **[Project configuration](configuration.md)** — the `dialogue.toml` that
  configures your project's speakers and default speaker, and how the CLI finds it.

> [!NOTE]
> The script language is still proposed syntax while the compiler is built out;
> the specification marks what is settled versus in progress.
