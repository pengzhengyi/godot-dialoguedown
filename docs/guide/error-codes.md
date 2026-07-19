# Error codes

DialogueDown reports each problem it finds as a **diagnostic** with a stable `DLG####` code, so a message is easy to look up. A code's leading digit names its category — `DLG1xxx` syntax, `DLG2xxx` semantic, `DLG3xxx` style — and each diagnostic has a default **severity**: **error** (must be fixed), **warning** (compiles but is suspect), or **info** (a neutral note). Placeholders such as `{0}` are filled with specifics — a name, a count — when the message is shown.

## Syntax (`DLG1xxx`)

A line's surface does not parse as intended.

### DLG1003

**Multiple jumps on a line** · Warning

This line has {0} jumps; multiple jumps on one line run in sequence and are easy to misread — prefer at most one.

### DLG1101

**Tags without a speaker** · Error

"{0}" has tags but names no speaker for them to attach to. Begin the line with a name to declare a speaker (Alice #excited:), or with an @id to add tags to an already-declared one (@alice #excited:).

### DLG1102

**Not a game call** · Error

"{0}" is not a game call. Write a query that reads a value ("key"), a default command (("do something")), or a named command (Name("arg", ...)).

### DLG1103

**Disallowed element in a label** · Error

{0} is not allowed inside a label or alt text; only text and styling are.

## Semantic (`DLG2xxx`)

A meaning-level problem found during analysis — a reference that does not resolve, or a conflict.

### DLG2001

**Duplicate scene anchor** · Error

Two scenes resolve to the same anchor '#{0}'. Rename one heading so each jump target is unambiguous.

### DLG2002

**Heading without an anchor** · Error

A heading needs at least one letter or number so it can be a jump target; this one has none. Add sluggable text to the heading.

### DLG2003

**Ambiguous speaker binding** · Error

Cannot bind name '{0}' to id '@{1}': both are already in use as separate speakers, so joining them now is ambiguous. If they are the same speaker, declare it (Name @{1}: …) before either is used on its own.

### DLG2004

**Id bound to two names** · Error

id '@{0}' is already bound to speaker '{1}', so it cannot also be bound to '{2}'. Use a different id for '{2}'.

### DLG2005

**Name bound to two ids** · Error

Speaker '{0}' is already bound to id '@{1}', so it cannot also be bound to id '@{2}'. Give the speaker a single id.

### DLG2006

**More than one default speaker** · Error

Two speakers are marked ##default ('{0}' and '{1}'); only one default speaker is allowed.

### DLG2007

**Unnamed speaker id** · Error

Speaker '@{0}' is used but never declared with a name. Declare it with a name (Name @{0}: …) — a stable id must belong to a named speaker.

### DLG2008

**Unknown reserved tag** · Error

'##{0}' is not a known reserved tag. Use a custom tag ('#{0}') or one of DialogueDown's reserved tags.

### DLG2009

**Jump to a missing scene** · Error

Jump target '#{0}' does not match any scene. Check the anchor, or add a heading it can point to.
