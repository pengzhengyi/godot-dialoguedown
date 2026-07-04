# Script language specification

This specification defines DialogueSystem's script language: a Markdown-subset
domain-specific language (DSL) for writing dialogue scripts. The language is
designed to stay readable for writers while still compiling into a precise graph
model for developers.

> [!NOTE]
> DialogueSystem is in early development. The script language, compiler model,
> and runtime behavior described here are subject to change as the library
> evolves.

## Table of contents

- [Script language specification](#script-language-specification)
  - [Table of contents](#table-of-contents)
  - [Why Markdown](#why-markdown)
  - [Goals](#goals)
  - [Processing model](#processing-model)
  - [Syntax summary](#syntax-summary)
  - [Text lines](#text-lines)
    - [Default speaker](#default-speaker)
    - [Inline speaker declaration](#inline-speaker-declaration)
    - [Whitespace around the colon](#whitespace-around-the-colon)
    - [Styling](#styling)
  - [Tags](#tags)
  - [Speaker references](#speaker-references)
  - [Dialogue structure](#dialogue-structure)
    - [Comments](#comments)
    - [Succession](#succession)
    - [Choices](#choices)
    - [Jumps](#jumps)
  - [Game-state integration](#game-state-integration)
    - [Queries](#queries)
    - [Commands](#commands)
  - [Complete example](#complete-example)
  - [File format](#file-format)

## Why Markdown

The script language intentionally uses a Markdown subset instead of a completely
custom text format.

- **Readable source:** Scripts remain easy to read before any compiler or editor
  integration exists.
- **Familiar syntax:** Writers and developers already know headings, lists,
  links, comments, and fenced blocks.
- **Editor support:** Markdown-aware editors provide highlighting, folding,
  outline navigation, snippets, and basic completion with little custom tooling.
- **Linting for free:** Existing Markdown tools can catch broken links, malformed
  headings, long lines, and formatting issues.
- **Git-friendly review:** Dialogue changes stay diffable and reviewable as
  plain text.

## Goals

The DSL is designed to be readable by writers while still compiling into a
precise runtime graph.

- Use plain text that works well in Git diffs and Markdown editors.
- Keep common dialogue terse: `Alice: Hello!`
- Support branching choices, jumps, tags, speaker declarations, game-state
  queries, and game-state commands.
- Preserve a clean boundary between dialogue content and engine-specific
  presentation.

## Processing model

```mermaid
flowchart TD
    Source[".dialogue.md source"] --> Parse["Parse syntax"]
    Parse --> Validate["Validate references"]
    Validate --> Compile["Compile to nodes and edges"]
    Compile --> Runtime["Run as a dialogue graph/state machine"]
```

## Syntax summary

| Feature | Example | Purpose |
| --- | --- | --- |
| Text line | `Alice: Hello, Bob!` | Speaker says a line. |
| Default speaker | `Hello from the narrator.` | Use the default speaker. |
| Inline speaker declaration | `Alice @A #main: Hello!` | Declare a speaker. |
| Speaker ID | `@A: Hello!` | Reference a stable speaker ID. |
| Tag | `#main` | Attach custom metadata. |
| Reserved tag | `##default` | Mark built-in behavior. |
| Choice | `- Bob: Really?` | Offer a selectable response. |
| Jump | `=> [Play tennis](#play-tennis)` | Connect to another section. |
| Query | `` `"Alice.FavoriteColor"` `` | Call `IGameSystem.Query`. |
| Default command | `` `("Alice joins Art")` `` | Call `IGameSystem.Execute`. |
| Custom command | `` `JoinClub("Alice", "Art")` `` | Execute with arguments. |

## Text lines

A text line is the basic unit of spoken dialogue.

```ebnf
TextLine = [ Speaker , ":" ] , Speech ;
```

Canonical form:

```markdown
Alice: Hello, Bob!
```

### Default speaker

When a line omits `Speaker`, the compiler will use the default speaker. If no
default speaker exists, it will use the system speaker.

```markdown
The narrator speaks because no explicit speaker is provided.
```

Mark a speaker as the default speaker with the reserved `##default` tag:

```markdown
Narrator @narrator ##default: The story begins.

This line is also spoken by Narrator.
```

### Inline speaker declaration

Inline speaker declarations are a lightweight way to introduce or enrich
speakers directly in script.

```ebnf
SpeakerName          = Identifier | String ;
SpeakerId            = Identifier ;
SpeakerDeclaration   = SpeakerName , [ "@" , SpeakerId ] , Tags ;
```

Example:

```markdown
Alice @A #main: Hello, Bob!
Bob @B #npc: Hello, Alice!
Alice #avatar="alice.png": The weather is nice today!
```

Inline declarations may appear multiple times as long as they don't conflict with
existing speaker identity. Conflicting speaker metadata is a compile-time error.

> [!NOTE]
> Speaker tags apply globally to the speaker, not just to the single text line
> where the tag appears.

### Whitespace around the colon

Whitespace around the colon is flexible for author comfort:

```markdown
Alice:Hello, Bob!

Alice :Hello, Bob!

Alice: Hello, Bob!

Alice : Hello, Bob!
```

If speech must start with a literal leading space, quote the speech instead of
depending on ambiguous whitespace:

```markdown
Alice: " Hello, Bob!"
```

### Styling

Speech may use standard Markdown emphasis for **styling**:

```markdown
Alice: I *really* mean it.

Alice: This is **very** important.
```

- `*text*` or `_text_` is **italic**; `**text**` or `__text__` is **bold**.
  Combine them (`***text***`) for bold italic.
- To type a **literal** asterisk or underscore, escape it (`\*`, `\_`).
  Underscores inside a word (`snake_case_name`) are never emphasis.

Styling can wrap other speech constructs — a query inside bold still resolves:

```markdown
Bob: **Hello `"MainCharacter.Name"`!**
```

renders a bold *Hello Alice!*.

> [!NOTE]
> This spec defines *what* styling an author can write. How a given style renders
> (color, bold weight, BBCode, plain text, …) is decided by the game's
> presentation layer, not by the compiler.

## Tags

Tags attach metadata that plugins, tools, or runtime systems can interpret.

```ebnf
TagName          = Identifier | String ;
TagGroupName     = Identifier | String ;
Tag              = "#" , TagName ;
TagGroup         = "#" , TagGroupName , "=" , TagName ;
ReservedTag      = "##" , TagName ;
ReservedTagGroup = "##" , TagGroupName , "=" , TagName ;
Tags             = { Tag | TagGroup | ReservedTag | ReservedTagGroup } ;
```

Examples:

```markdown
Alice @A #main: Hello, Bob!

Alice #mood=happy: What a beautiful day.

Alice #"speaker tone"="warm": I'm glad to see you.

Narrator @narrator ##default: The story begins.
```

Regular tags (`#...`) are project-defined custom metadata. Reserved tags
(`##...`) are built-in language tags owned by DialogueSystem.

Tags must attach to another syntax element, such as a speaker declaration. A
regular or reserved tag must never start a line at block scope.

Currently, the only supported reserved tag is `##default`, which marks a speaker
as the default speaker.

## Speaker references

Speakers can be referenced by name or by stable ID.

```ebnf
SpeakerReference = SpeakerName | "@" , SpeakerId ;
```

Examples:

```markdown
Alice: Hello, Bob!

@A: Hello, Bob!
```

Stable IDs are useful when a character has nicknames, localized names, or
multiple display names.

For long-form stories, keep a central `speakers.json` file so speaker identity
and metadata have a single source of truth.

```json
[
  {
    "name": "Alice",
    "id": "A",
    "tags": ["main"]
  },
  {
    "name": "Bob",
    "id": "B",
    "tags": ["npc"]
  }
]
```

## Dialogue structure

Dialogue sections become graph nodes and edges. Linear lines create succession
edges. Choices and jumps create branches.

### Comments

Because the DSL is Markdown-inspired, use Markdown-compatible HTML comments for
author notes.

```markdown
Alice @A #main: Hello, Bob! <!-- Alice speaks in a warm tone. -->

Bob @B #npc: Hello, Alice!
```

### Succession

When one text line follows another, the second line is the only successor of the
first line. Separate successive speeches with a **blank line** so that each
speech is its own Markdown paragraph.

```markdown
Alice @A #main: Hello, Bob!

Bob @B #npc: Hello, Alice!
```

The script language follows standard Markdown line-break rules, so a Markdown
preview groups lines into speeches exactly as the compiler does:

- A **blank line** starts a new speech. This is the primary, most readable way to
  separate successive speeches.
- A **soft break** (a plain newline with no blank line) keeps both lines in the
  same speech. Use it to wrap one long speech across several source lines.
- A **hard break** starts a new speech without a blank line, for a compact
  layout. Make a break hard in either of the two standard Markdown ways: end the
  line with two or more trailing spaces, or end it with a backslash (`\`).

A soft break wraps a single speech across source lines; it is still one speech:

```markdown
Alice: This is a single long speech that the author wrapped across
several source lines for readability. It is still spoken as one speech.
```

A hard break separates two speeches without a blank line. The trailing backslash
below is one of the two hard-break forms; two trailing spaces is the other:

```markdown
Alice: Hello, Bob!\
Bob: Hello, Alice!
```

### Choices

Use `-` to offer selectable responses.

```markdown
Alice: The weather is nice today!
- Bob: Is it really?
- Bob: Yes, I agree.
```

Choices can be nested, but deep nesting becomes hard to scan. Prefer jumps when
branches split and later merge again.

```markdown
Alice: The weather is nice today!
- Bob: Is it really?
    - Alice: Yes. Let's play tennis!
- Bob: Yes, I agree.
    - Alice: Wonderful. Let's play tennis!
```

### Jumps

A jump is `=>` followed by a Markdown-style link.

```ebnf
Jump = "=>" , MarkdownLink ;
```

Use same-file anchors for local dialogue and relative paths for cross-file
dialogue.

```markdown
=> [Play tennis](#play-tennis)
=> [Meet Bob](chapter-02.md#meet-bob)
```

Example:

```markdown
## Greetings

Alice: The weather is nice today!
- Bob: Is it really?
    - Alice: Yes. Let's play tennis!
        => [Play tennis](#play-tennis)
- Bob: Yes, I agree.
    - Alice: Wonderful. Let's play tennis!
        => [Play tennis](#play-tennis)

## Play tennis

Alice: Tennis is fun!

Bob: Yes, I agree.
```

## Game-state integration

The current `IGameSystem` interface exposes two integration points:

```csharp
public interface IGameSystem
{
    string Query(string query);

    void Execute(string command);
}
```

The DSL will compile query and command syntax into calls to that adapter.

### Queries

A query reads game state and inserts the returned value into speech.

```ebnf
Query = "`" , QuotedString , "`" ;
```

Adapter example:

```csharp
public sealed class GameSystem : IGameSystem
{
    public string Query(string query)
    {
        return query switch
        {
            "Alice.FavoriteColor" => "red",
            _ => string.Empty
        };
    }

    public void Execute(string command)
    {
    }
}
```

Script:

```markdown
Bob: What's your favorite color?

Alice: My favorite color is `"Alice.FavoriteColor"`.
```

Actual speech after query resolution:

```markdown
Bob: What's your favorite color?

Alice: My favorite color is red.
```

### Commands

A command changes game state through `IGameSystem.Execute`.

```ebnf
DefaultCommand = "`" , "(" , QuotedString , ")" , "`" ;
CustomCommand  = "`" , Identifier , "(" , [ Arguments ] , ")" , "`" ;
Command        = DefaultCommand | CustomCommand ;
```

Adapter example:

```csharp
public sealed class GameSystem : IGameSystem
{
    public string Query(string query)
    {
        return string.Empty;
    }

    public void Execute(string command)
    {
        switch (command)
        {
            case "JoinClub(\"Alice\", \"Kung Fu\")":
                JoinClub("Alice", "Kung Fu");
                return;
        }
    }

    private static void JoinClub(string characterName, string clubName)
    {
        // Update game state here.
    }
}
```

Default command:

```markdown
Bob: Of course. You can join. `("Alice joins Kung Fu")`

Alice: Thank you!
```

Custom command:

```markdown
Bob: Of course. You can join. `JoinClub("Alice", "Kung Fu")`

Alice: Thank you!
```

Silent command:

```markdown
Alice: Bob, do you have a minute?

Bob: Yes. What can I do for you?

Alice: I like Chinese martial arts. Can I join the Kung Fu Club?

Bob: Of course.

`JoinClub("Alice", "Kung Fu")`

Alice: Thank you!
```

Under the hood, a silent command will compile to a command-only text line spoken
by the default speaker.

```markdown
@default: `JoinClub("Alice", "Kung Fu")`
```

The compiler will emit a special node for each game-system call. The node shape
and runtime execution contract are outside this document's scope.

## Complete example

```markdown
## Gallery

- Alice: Bob, this is your photo. I love it!
    => [Discuss Bob's photo](#discuss-bobs-photo)
- Alice: Look, this is Christina's painting.
    => [Discuss Christina's painting](#discuss-christinas-painting)

## Discuss Bob's photo

Bob: Thank you. I'm glad you like it. `IncreaseAffection("Bob", "Alice")`

Alice: I want to join the Photography Club!

Bob: Good idea. You'll meet new friends and have fun.

`JoinClub("Alice", "Photography")`

## Discuss Christina's painting

Bob: This is the night view of the Huangpu River. It's beautiful.

Alice: I love this painting too. The colors are amazing.

Christina: I learned color theory in the Art Club.

`IncreaseAffection("Christina", "Alice")`

Alice: I'd like to join the Art Club and give painting a try.

`JoinClub("Alice", "Art")`
```

## File format

Save dialogue scripts with the `.dialogue.md` extension.

This keeps the file readable as Markdown while making it clear that the file is
dialogue source, not ordinary project documentation.

Example filenames:

- `chapter-01.dialogue.md`
- `intro.dialogue.md`
- `npc/shopkeeper.dialogue.md`

Use normal Markdown tooling for editing and review. The compiler will treat these
files as DialogueSystem script files.
