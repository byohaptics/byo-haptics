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
2. Select the first unchecked task whose prerequisites are complete.
3. Make the smallest implementation that satisfies that task.
4. Run `npm test`.
5. Run an over-engineering review and remove unnecessary complexity.
6. Update the task and handoff documents.
7. Commit one coherent change.
8. Continue unless user input or a live Resonite test is required.

Do not deploy when Resonite is unavailable or when the user has prohibited deployment.

## Engineering Rules

- Do not copy product SlotSpecs, ProtoGraphs, generated artifacts, or Git history from the private prototype. Generic tooling may be migrated after publication-policy review.
- Do not embed session-specific IDs, ports discovered at runtime, or world-specific references.
- Keep device behavior inside output plugins; the host remains transport-neutral.
- Use SlotSpec and ProtoGraph sources as reproducible inputs. Generated scene IDs are disposable build artifacts.
- Prefer one execution path for each lifecycle action.
- Avoid speculative abstractions and dependencies.
- Never commit generated credentials, inventory exports, logs, or local machine paths.

## Publication Gate

Run `npm test` before every commit. The local publication policy hook must pass before any push. Do not push until the license decision and release checklist are complete.
