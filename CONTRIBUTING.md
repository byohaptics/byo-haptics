# Contributing

Read [repository instructions](AGENTS.md), the relevant specifications and [known limitations](docs/limitations.md) before changing implementation.

## Change Process

1. Update the governing specification when behavior is missing or ambiguous.
2. Keep Host behavior transport-neutral and device behavior inside an Output Plugin or Bridge.
3. Do not commit runtime IDs, generated scene artifacts, local paths, logs, device identifiers, or credentials.
4. Run the checks documented in `docs/building.md`.
5. Run an over-engineering review and remove unused flexibility.
6. Submit one coherent change with its verification result.

For live behavior changes, include the tested version, scenario, result and remaining limits in the change description after VR or hardware verification. Keep account/world identities, raw logs and session records private.
