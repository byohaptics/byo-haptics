# Autonomous Development Loop

Document version: `0.1.0`

## Loop

1. Read `docs/current-handoff.md`.
2. Select the first unchecked item in `docs/todo.md` whose prerequisites are complete.
3. Confirm the governing specification and acceptance condition.
4. Implement only that item.
5. Run `npm test` and any task-specific static checks.
6. Run `ponytail-review` on the diff and remove dead flexibility, duplicate writers, and one-time migration code.
7. Update specifications first if implementation uncovered a missing requirement.
8. Update the task and handoff.
9. Commit with the local `byohaptica` identity.
10. Continue automatically unless a live test, account action, hardware action, or product decision is required.

## Commit Boundary

Each commit must leave generated inputs reproducible and static checks passing. Do not mix specification corrections, unrelated UI changes, and device behavior in one commit.

## Live Test Boundary

Codex prepares and deploys only when Resonite is running and deployment is explicitly allowed. A user performs VR and hardware observations. The result is recorded in `docs/test-results.md`, including user, world ownership, plugin, Target, and observed output.

## Specification Feedback

If a test fails because the implementation differs from a complete specification, fix the implementation. If two reasonable implementations satisfy the document but only one works, the specification is incomplete: record the discovered constraint before fixing code.

## Stop Conditions

Stop and request input only for:

- license selection;
- creation or publication of the GitHub repository;
- login, account, or secret configuration;
- live Resonite, multi-user, or device tests;
- an irreversible product or compatibility decision.
