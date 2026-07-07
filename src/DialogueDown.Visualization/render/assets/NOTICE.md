# Third-party vendored assets

These client libraries are bundled verbatim so the generated HTML reports work
fully offline, with no CDN or network access required. Each report prefers the
latest copy from a CDN and falls back to the vendored copy below when the CDN is
unreachable. None of these is a NuGet or runtime dependency of the DialogueDown
packages; they ship only inside generated report HTML.

| Library | Version | License | SHA-256 |
| --- | --- | --- | --- |
| [D3.js](https://d3js.org) | 7.9.0 | ISC | `f2094bbf6141b359722c4fe454eb6c4b0f0e42cc10cc7af921fc158fceb86539` |
| [Pico.css](https://picocss.com) | 2.0.6 | MIT | `dd5fd5591afd81ee21dcc117ad85c014dc3f1f19dc2d7b7d101ea0acc29274c2` |
| [marked](https://marked.js.org) | 12.0.2 | MIT | `15fabce5b65898b32b03f5ed25e9f891a729ad4c0d6d877110a7744aa847a894` |

Source URLs (pinned):

- `https://cdn.jsdelivr.net/npm/d3@7.9.0/dist/d3.min.js`
- `https://cdn.jsdelivr.net/npm/@picocss/pico@2.0.6/css/pico.min.css`
- `https://cdn.jsdelivr.net/npm/marked@12.0.2/marked.min.js`

Updating any of these is a deliberate, reviewable file swap: replace the file,
then update its version and SHA-256 above.

## Licenses

### D3.js — ISC

```text
Copyright 2010-2023 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
```

### Pico.css and marked — MIT

Both are distributed under the MIT License (Copyright 2019-2024 Pico.css
contributors, and Copyright 2011-2024 Christopher Jeffrey, respectively). The MIT
License permits use, copy, modification, and distribution provided the copyright
and permission notice are retained; the full notice is preserved in each vendored
file's header and at the projects' repositories.
