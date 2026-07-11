---
applyTo: "docs/**/*.md"
---

# Documentation conventions

The `docs/` tree is **audience-first** and builds into a DocFX site:

- **`docs/guide/`** — writer-facing: the overview and the script-language spec.
- **`docs/contributing/design-notes/`** — one design note per component and
  compiler stage, each recording the goal, key decisions, and tradeoffs.
- **`docs/api/`** — the generated C# API reference (do not hand-edit).

## Writing

- **American English**; keep prose tight; use sentence-style headings and a table
  of contents on longer notes.
- **Link to the authoritative doc** rather than restating build steps, conventions,
  or API details — point at `CONTRIBUTING.md`, the design notes, or the API
  reference so the docs never drift.
- Use **Mermaid diagrams** to clarify flow, architecture, and state; keep each
  diagram small and maintainable. Prefer a diagram over a long paragraph when it
  reads faster.
- **Polish the writing:** active voice, short paragraphs, concrete examples. Keep
  Markdown clean for `markdownlint` and links valid for `lychee`.
- A design note opens with a status callout (`> [!NOTE]` proposed / in progress /
  implemented) and is written as the current design, not a changelog.

## How to add a design note

1. Create `docs/contributing/design-notes/<Note Name>.md` with a status callout
   and the note's goal, key decisions, and tradeoffs.
2. Add a row for it to the index table in
   `docs/contributing/design-notes/README.md`.
3. Register it in `docs/contributing/design-notes/toc.yml` so it appears in the
   site sidebar (`- name:` + `href:`).
4. Build the site to confirm it renders and links resolve:

   ```bash
   dotnet tool restore
   dotnet tool run docfx docs/docfx.json           # add --serve to preview locally
   ```

The generated `docs/_site/` and `docs/api/*.yml` are ignored — never commit them.
