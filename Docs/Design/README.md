# Big Retail Design Hub

This folder is the canonical design-memory system for Big Retail.

## How we use it

- `CURRENT.md` is the first file to read when asking: **"Where are we at?"**
- Stable systems get their own topic file, such as `Suppliers.md`.
- When a major design decision is approved, update the relevant topic file and then update `CURRENT.md`.
- Keep `CURRENT.md` short. It is a status board, not the full design document.
- Topic files hold the durable reasoning and current accepted design.
- Deferred ideas should be labeled **Deferred**, not mixed into the current implementation target.

## Chat shorthand

When Jesse says:

- **"Where are we at?"** → read `CURRENT.md` and summarize the active design state.
- **"Lock that"** → update the relevant topic file and `CURRENT.md`.
- **"Add that to resources"** → save the accepted design into this design hub unless the request clearly refers to a Unity runtime asset.
- **"What did we decide about X?"** → read the relevant topic file first, then answer.

## Important Unity distinction

Design documents belong here under `Docs/Design/`, not in Unity's special `Assets/Resources/` runtime folder. `Assets/Resources/` should remain reserved for runtime-loaded Unity assets when the project genuinely needs it.

## Source-of-truth rule

`Docs/Design/` is the current design authority. Older design notes elsewhere in the repository may contain stale assumptions and should be treated as reference material until reconciled into this folder.
