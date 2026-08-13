# Autonomous Development Loop

Document version: `0.1.5`

## Loop

1. Read `docs/current-handoff.md`.
2. Select the first unchecked item in `docs/todo.md` whose prerequisites are complete.
3. Confirm the governing specification and acceptance condition.
4. Implement only that item.
5. Run `npm test` and any task-specific static checks.
6. Run `ponytail-review` on the diff and remove dead flexibility, duplicate writers, and one-time migration code.
7. Update specifications first if implementation uncovered a missing requirement.
8. Update the task and handoff.
9. Commit with the local `byohaptics` identity.
10. Continue automatically unless a live test, account action, hardware action, or product decision is required.

## Commit Boundary

Each commit must leave generated inputs reproducible and static checks passing. Do not mix specification corrections, unrelated UI changes, and device behavior in one commit.

## Host Commands

- `npm run compile:host` compiles all eight host sheets without changing a scene.
- `npm run deploy:host` builds the SlotSpec and deploys all host sheets. It requires a running ResoniteLink endpoint and refuses to deploy unless the wrapper sets the explicit deployment gate.
- `npm run deploy:host -- -Recreate` removes an existing World Root host with the same tool name before rebuilding it.
- `npm run compile:plugins` compiles all three public Plugin graphs without changing a scene.
- `npm run deploy:joycon`, `npm run deploy:haptira`, and `npm run deploy:demo` build and deploy one Plugin Package. To replace the corresponding World Root package on Windows, call `scripts/deploy-plugin.ps1 -Plugin <joycon|haptira|demo> -Recreate` directly.

Generated IDs are written under `build/` and are valid only for the current scene build. They are never committed.

## Live Test Boundary

Codex prepares and deploys only when Resonite is running and deployment is explicitly allowed. A user performs VR and hardware observations. The result is recorded in `docs/test-results.md`, including user, world ownership, plugin, Target, and observed output.

## Specification Feedback

If a test fails because the implementation differs from a complete specification, fix the implementation. If two reasonable implementations satisfy the document but only one works, the specification is incomplete: record the discovered constraint before fixing code.

## Stop Conditions

Stop and request input only for:

- changes to the selected MIT license;
- creation or publication of the GitHub repository;
- login, account, or secret configuration;
- live Resonite, multi-user, or device tests;
- an irreversible product or compatibility decision.
