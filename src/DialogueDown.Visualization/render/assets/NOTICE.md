# Third-party vendored asset — D3.js

`d3.v7.min.js` is bundled verbatim so the generated HTML reports work fully
offline, with no CDN or network access. It is not a NuGet or runtime dependency
of the DialogueDown packages; it ships only inside generated report HTML.

| Field | Value |
| --- | --- |
| Library | D3.js |
| Version | 7.9.0 (pinned) |
| Source | <https://cdn.jsdelivr.net/npm/d3@7.9.0/dist/d3.min.js> |
| SHA-256 | `f2094bbf6141b359722c4fe454eb6c4b0f0e42cc10cc7af921fc158fceb86539` |
| License | ISC |

Updating D3 is a deliberate, reviewable file swap: replace `d3.v7.min.js`, then
update the version and SHA-256 above.

## License

D3 is distributed under the ISC License, reproduced below.

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
