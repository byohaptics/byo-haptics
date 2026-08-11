# Repository Instructions

## Objective

Rebuild BYO Haptics, the Joy-Con OSC output plugin, and the Haptira OSC output plugin from the public specifications in this repository.

## Source Of Truth

1. `docs/project-scope.md`
2. `docs/output-plugin-contract.md`
3. Device-specific specifications
4. `docs/todo.md`

Implementation must follow the specifications. When a test exposes missing or incorrect requirements, update the specification before changing implementation.

## Development Loop

1. Read `docs/current-handoff.md` and `docs/todo.md`.
2. Confirm `git config core.hooksPath` is `.githooks` and `PUBLICATION_DENYLIST` is available.
3. Select the first unchecked task whose prerequisites are complete.
4. Make the smallest implementation that satisfies that task.
5. Run `npm test`.
6. Run an over-engineering review and remove unnecessary complexity.
7. Update the task and handoff documents.
8. Commit one coherent change.
9. Continue unless user input or a live Resonite test is required.

Do not deploy when Resonite is unavailable or when the user has prohibited deployment.

## Engineering Rules

- Declarative sources and generic tooling may be migrated only after publication-policy review, version reset, and removal of private or runtime-specific data.
- Do not copy generated scene artifacts, inventory exports, session IDs, or Git history from the private prototype.
- Do not embed session-specific IDs, ports discovered at runtime, or world-specific references.
- Keep device behavior inside output plugins; the host remains transport-neutral.
- Use SlotSpec and ProtoGraph sources as reproducible inputs. Generated scene IDs are disposable build artifacts.
- Prefer one execution path for each lifecycle action.
- Avoid speculative abstractions and dependencies.
- Never commit generated credentials, inventory exports, logs, or local machine paths.

## Publication Gate

Run `npm test` before every commit. The local publication policy hook must pass before any push. Do not push until the license decision and release checklist are complete.
