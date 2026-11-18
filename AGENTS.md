# Repository Instructions

## Objective

Rebuild BYO Haptics, the Joy-Con OSC output plugin, and the Haptira OSC output plugin from the public specifications in this repository.

## Source Of Truth

1. `docs/project-scope.md`
2. `docs/output-plugin-contract.md`
3. Device-specific specifications
4. The issue or change request being implemented

Implementation must follow the specifications. When a test exposes missing or incorrect requirements, update the specification before changing implementation.

## Development Loop

1. Read the applicable specifications and change request. In a maintainer checkout, consult `.tools/maintainer-docs/README.md` when present for private task/handoff records.
2. Confirm `git config core.hooksPath` is `.githooks` and `PUBLICATION_DENYLIST` is available.
3. Select an authorized task whose prerequisites are complete.
4. Make the smallest implementation that satisfies that task.
5. Run `npm test`.
6. Run an over-engineering review and remove unnecessary complexity.
7. Update public specifications/limitations and record verification in the change description. Keep maintainer tasks and handoffs outside the public tree.
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

Run `npm test` before every commit. The local publication policy hook must pass before any push. Do not push until the license decision and the maintainer release review are complete.
