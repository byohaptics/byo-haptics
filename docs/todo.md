# Build Plan And Tasks

Document version: `0.1.0`

Tasks are executed from top to bottom. A checked task must have a verification result or commit.

## Phase 0: Repository Foundation

- [x] Create a history-free local repository.
- [x] Configure local commit identity as `byohaptica <byohaptics@gmail.com>`.
- [x] Set product, plugin, and document versions to `0.1.0`.
- [x] Define the autonomous development loop.
- [x] Add baseline static checks.
- [ ] Select and add a distribution license. Requires copyright-holder decision.
- [ ] Create the GitHub repository under the `byohaptics` organization. Requires account action.

## Phase 1: Public Specifications

- [x] Define project scope and architecture.
- [x] Define Output Plugin contract version 1.
- [x] Define Plugin Package lifecycle and direct drop behavior.
- [x] Define Joy-Con OSC plugin behavior.
- [x] Define Haptira OSC plugin behavior.
- [x] Define complete SlotSpec schema used by this repository.
- [x] Define host UI slot names, dimensions, controls, and references.
- [x] Define host lifecycle and context menu state tables.
- [x] Define sampler row state and offset equations.

## Phase 2: Build Toolchain

- [x] Add SlotSpec builder without runtime ID fallbacks.
- [x] Add generic ProtoGraph compile tooling.
- [ ] Add host deployment after the first host SlotSpec and sheet exist.
- [x] Add generated-ID and fixed-ID validation.
- [x] Add version synchronization for host and both plugins.
- [ ] Add validators for host and Plugin Package specs.

## Phase 3: Host Reconstruction

- [ ] Create host SlotSpec from the public UI and structure specifications.
- [ ] Implement lifecycle and positioning sheets.
- [ ] Implement source binding and sampler sheets.
- [ ] Implement Output Bus, plugin discovery, and Package Manager sheets.
- [ ] Implement read-only diagnostics sheet.
- [ ] Compile all sheets and validate references.

## Phase 4: Plugin Reconstruction

- [ ] Build the Joy-Con OSC Plugin Package from its specification.
- [ ] Build the Haptira OSC Plugin Package from its specification.
- [ ] Verify each plugin compiles without host-specific IDs.
- [ ] Verify card direct drop and ejection use one Package Manager path.

## Phase 5: Static Review

- [ ] Run all validators.
- [ ] Run the local publication policy check.
- [ ] Run `ponytail-review` and apply accepted reductions.
- [ ] Commit the reproducible static build.

## Phase 6: Live Verification

- [ ] Test under World Root while logged in as `byohaptica`.
- [ ] Test install, uninstall, grab, close, and panel reset.
- [ ] Test Node and Slot source modes and null-source behavior.
- [ ] Test sampler edit, offset persistence, and reset.
- [ ] Test Joy-Con Force, Vibration, Pain, heartbeat, and bridge loss.
- [ ] Test Haptira channels `00`, `01`, `02`, and an upper channel.
- [ ] Test plugin replacement, ejection, inventory save, and another world.
- [ ] Test two users and another user's world.

## Phase 7: Publication

- [ ] Complete release checklist.
- [ ] Confirm no private history, local IDs, logs, or inventory artifacts exist.
- [ ] Tag `v0.1.0` only after live acceptance passes.
- [ ] Push and publish documentation.
