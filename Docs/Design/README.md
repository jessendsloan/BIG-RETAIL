# Big Retail Design Hub

This folder is the canonical design-memory system for Big Retail.

## How we use it

- `CURRENT.md` is the first file to read when asking: **"Where are we at?"**
- Stable systems get their own topic file, such as `Suppliers.md`.
- When a major design decision is approved, update the relevant topic file and then update `CURRENT.md`.
- Keep `CURRENT.md` short. It is a status board, not the full design document.
- Topic files hold the durable reasoning and current accepted design.
- Deferred ideas should be labeled **Deferred**, not mixed into the current implementation target.
- `Patches/` holds promising deferred designs that are intentionally **not active yet**. A patch preserves an idea so it can be revisited later without contaminating the current design target.

## Organization stewardship

The assistant is responsible for keeping this design hub orderly as the project grows.

These documents are primarily working memory for future design conversations and implementation handoffs, so optimize them for fast retrieval and unambiguous state while keeping them comfortable for a human to read.

Default organization rules:

- Prefer a small number of durable topic files over many tiny notes.
- Keep one clear source of truth for each active system.
- Separate **Active**, **Deferred**, **Open Question**, and **Patch** ideas rather than blending them together.
- Do not duplicate accepted design across several files unless one location is explicitly a short summary/index.
- When an old decision is replaced, update the canonical topic file instead of preserving contradictory active versions.
- Preserve promising future ideas as patches rather than forcing them into the current implementation scope.
- Create new folders or indexes only when the existing structure has genuinely become hard to navigate.
- Favor descriptive filenames that remain obvious months later.
- Keep implementation details separate from design intent when that distinction helps future work.
- Treat organization itself as maintenance work: tidy stale references, broken links, duplicate notes, and outdated status summaries when encountered.

The goal is not bureaucratic documentation. The goal is to make it possible to ask **"Where are we at?"** at any time and recover the current design quickly and accurately.

## Chat shorthand

When Jesse says:

- **"Where are we at?"** → read `CURRENT.md` and summarize the active design state.
- **"Lock that"** → update the relevant topic file and `CURRENT.md`.
- **"Add that to resources"** → save the accepted design into this design hub unless the request clearly refers to a Unity runtime asset.
- **"Save that as a patch"** → preserve the idea under `Docs/Design/Patches/` and keep it out of the active implementation target unless explicitly promoted later.
- **"What did we decide about X?"** → read the relevant topic file first, then answer.

## Important Unity distinction

Design documents belong here under `Docs/Design/`, not in Unity's special `Assets/Resources/` runtime folder. `Assets/Resources/` should remain reserved for runtime-loaded Unity assets when the project genuinely needs it.

## Source-of-truth rule

`Docs/Design/` is the current design authority. Older design notes elsewhere in the repository may contain stale assumptions and should be treated as reference material until reconciled into this folder.

Files under `Docs/Design/Patches/` are preserved possibilities, not current authority. They only become active design when explicitly promoted into a topic file and reflected in `CURRENT.md`.
