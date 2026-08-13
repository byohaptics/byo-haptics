# Build Plan And Tasks

Document version: `0.1.0`

Tasks are executed from top to bottom. A checked task must have a verification result or commit.

## Phase 0: Repository Foundation

- [x] Create a history-free local repository.
- [x] Configure local commit identity as `byohaptica <byohaptics@gmail.com>`.
- [x] Set product, plugin, and document versions to `0.1.0`.
- [x] Define the autonomous development loop.
- [x] Add baseline static checks.
- [x] Select and add the MIT distribution license.
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
- [x] Add a type-checked host deployment path after the first host SlotSpec and sheets exist.
- [x] Add generated-ID and fixed-ID validation.
- [x] Add version synchronization for host and both plugins.
- [x] Add a validator for the host SlotSpec.
- [x] Add validators for both Plugin Package specs.
- [x] Configure Windows CI with pinned FluxSDK and Bridge linting.
- [x] Add a sanitized Windows package build for the Joy-Con Bridge CLI and GUI.

## Phase 3: Host Reconstruction

- [x] Create the sanitized, version-reset host SlotSpec from the public UI and structure specifications.
- [x] Implement and compile the lifecycle sheet.
- [x] Implement and compile the positioning sheet.
- [x] Implement and compile source binding and sampler sheets.
- [x] Implement and compile Output Bus, plugin discovery, and Package Manager sheets.
- [x] Implement and compile the read-only diagnostics sheet.
- [x] Compile all sheets and validate SlotSpec references.

## Phase 4: Plugin Reconstruction

- [x] Build and compile the Joy-Con OSC Plugin Package from its specification.
- [x] Build and compile the Haptira OSC Plugin Package from its specification.
- [x] Verify each plugin compiles without host-specific IDs.
- [x] Import the Joy-Con Bridge source without private history, logs, traces, or hardware identifiers.
- [x] Reset the Joy-Con Bridge and Bridge API to `0.1.0`.
- [x] Build and test the Joy-Con Bridge CLI and GUI.
- [x] Verify card direct drop and ejection use one Package Manager path.

## Phase 5: Static Review

- [x] Run all validators.
- [x] Run the local publication policy check.
- [x] Run `ponytail-review` and apply accepted reductions.
- [x] Commit the reproducible static build.
- [x] Verify the Bridge release archive contains no private build path or hardware identifier.
- [x] Audit Host, both Plugin Packages, and Bridge sources against the current functional baseline.

## Phase 6: Live Verification

- [x] Test under World Root while logged in as `byohaptica`.
- [x] Test install, uninstall, grab, close, and panel reset.
- [ ] Test Node and Slot source modes and null-source behavior.
- [x] Test sampler edit, offset persistence, and reset.
- [ ] Test Joy-Con Force, Vibration, Pain, heartbeat, and bridge loss.
- [ ] Test Haptira channels `00`, `01`, `02`, and an upper channel.
- [ ] Test plugin replacement, ejection, inventory save, and another world.
- [x] Test both plugins from both users in a two-user session.
- [ ] Test installation and output in another user's world.

## Phase 7: Publication

- [ ] Complete release checklist.
- [ ] Confirm no private history, local IDs, logs, or inventory artifacts exist.
- [ ] Tag `v0.1.0` only after live acceptance passes.
- [ ] Push and publish documentation.
