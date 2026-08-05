# Big Retail Development Workflow

This is our shared playbook for working quickly without making the Unity
project fragile. Jesse does not need to remember the technique names; Codex
should propose the right one when a task matches its trigger.

## Tight Local Loop

Use for normal code and asset-file work.

```text
Describe a goal
→ Codex updates the local project
→ Unity recompiles automatically
→ Jesse playtests in the real Editor
→ tests pass
→ commit the accepted checkpoint
```

**Use when:** a change can be made safely in project files without manually
editing a scene or prefab.

**Human gate:** Unity playtesting and visual acceptance.

## Editor Bootstrapper

A small `Big Retail/...` Unity menu command that performs stable, repeatable
editor setup in one action.

```text
One menu command
→ create or preserve authored assets
→ apply known-safe import settings
→ add and wire standard scene components
→ select the result for inspection
```

**Use when:** the same Inspector setup would otherwise be repeated, the
created assets have known defaults, and the command can preserve existing
authored work when run again.

**Examples:** initial catalogs, standard runtime hosts, toolbar presenters,
and test content.

**Do not use when:** the task needs creative scene composition, intentional
one-off layout decisions, or destructive replacement of authored content.

## One Boundary at a Time

Build Unity vertical slices in small, independently testable checkpoints:

```text
domain rules → runtime host → empty view → real presentation → input → UI
```

After each boundary, compile, test, and use Play Mode before adding the next.
If a scene integration fails, recover to the last accepted checkpoint rather
than layering guesses on top of it.

## Commit Discipline

- Never commit broken or untested Unity state.
- Keep unrelated art, code, and scene changes separated when practical.
- Treat `main` as the accepted game.
- Feature branches are safe workspaces; pull requests are acceptance gates.
- Unity-generated `.meta` files are part of the project and belong with their
  corresponding asset or script.

## Roles

**Jesse:** creative direction, art, playtesting, visual judgment, and final
approval.

**Codex:** architecture, implementation, tests, safe Git coordination, and
recognizing when a bootstrapper or isolated checkpoint will save work.
