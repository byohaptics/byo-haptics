# Build Plan And Tasks

Document version: `0.1.9`

Tasks are executed from top to bottom. A checked task must have a verification result or commit.

## Phase 0: Repository Foundation

- [x] Create a history-free local repository.
- [x] Configure local commit identity as `byohaptics <byohaptics@gmail.com>`.
- [x] Set product, plugin, and document versions to `0.1.0`.
- [x] Define the autonomous development loop.
- [x] Add baseline static checks.
- [x] Select and add the MIT distribution license.
- [x] Create the private GitHub repository under the `byohaptics` account.

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
- [x] Test Node and Slot source modes and null-source behavior.
- [x] Test sampler edit, offset persistence, and reset.
- [x] Test Joy-Con Force, Vibration, and Pain output.
- [x] Test Bridge shutdown and Link down indication.
- [x] Test Link recovery after Joy-Con Bridge restart.
- [x] Test device-output recovery after Joy-Con Bridge restart.
- [x] Test Haptira channels `00`, `01`, `02`, and an upper channel.
- [x] Test plugin replacement, ejection, inventory save, and another world.
- [x] Test both plugins from both users in a two-user session.
- [x] Test installation and output in another user's world.

## Phase 7: Publication

- [x] Complete release checklist.
- [x] Confirm no private history, local IDs, logs, or inventory artifacts exist.
- [x] Tag `v0.1.0` after live acceptance passes.
- [x] Push the private repository and release documentation.

## Post-release Maintenance

- [x] Add the Host license policy to both Plugin Package SlotSpecs and static checks.
- [x] Use stable row-index labels in Sampler Edit and the Host panel, and distinguish configured sources from currently resolvable sources.
- [x] Retain each positive sampler sensation for a configurable minimum duration, defaulting to 80 ms.
- [x] Confirm in VR that a short collision remains perceptible for the configured minimum duration.
- [x] Apply the repository-wide Ponytail audit by removing unused Host deployment recovery paths and the Bridge's test-only random abstraction.
- [x] Stop a nonfunctional Joy-Con Bridge after HID failure and expose controller, service, and Plugin connection states in its GUI.
- [x] Prevent a stopped Joy-Con Bridge from presenting a stale manual detection snapshot as a live controller connection.
- [ ] Verify Joy-Con disconnect handling and all three Bridge GUI status indicators with live hardware.
- [x] Repeat Haptira channel state every 50 ms while active and for a 250 ms zero tail to tolerate UDP loss.
- [ ] Verify repeated GunRecoil samples no longer leave a Haptira motor running.
- [x] Reduce the Host from 2088 to 1588 live slots by deleting diagnostic mirrors and merging Source Binding into Sampler.
- [ ] Regression-test clear, Slot sources, offsets, plugin status, and 80 ms pulse retention on the lightweight Host.
- [x] Replace the Output Bus's 16 scalar hold states with four row-local `float4` vectors without sharing sensation expiry.
- [x] Deploy the vectorized Output Bus to `byohaptics World` and verify its six sheets, 80 ms default, and independent row hold-vector state by live readback.
- [x] Confirm manual Slot source mode on the deployed vectorized Host.
- [ ] Confirm in VR that Force does not retain Vibration and the configured 80 ms minimum remains perceptible after vectorization.
- [x] Consolidate internal Host state Slots and duplicate full-stretch UI wrappers, then remove unused configuration and constant visibility state, reducing the source SlotSpec from 245 Slots and 650 components to 174 Slots and 597 components.
- [x] Deploy the compacted Host and measure its live total at 1331 slots.
- [x] Verify Node source mode and Demo Output Plugin behavior on the compacted Host.
- [ ] Visually inspect the compacted panel and regression-test Slot sources, clear, offsets, plugin status, and pulse retention.
- [ ] Rebuild and redeploy both Plugin Packages, then verify their root `License` components live.

## Demo Plugin

- [x] Specify direct scene-field connection, `left` and `right` routing, and simulated vibration.
- [x] Build and compile the Demo Plugin Package and both simulated devices.
- [x] Validate direct drop, independent `left` and `right` routing, and vibration live.
- [x] Match the Joy-Con card style with a distinct accent, increase vibration visibility, and keep installed devices clear of the Host panel.
- [ ] Visually accept the compact card spacing, stronger vibration, and device placement in `byohaptics World`.
- [x] Rebuild and redeploy Demo v0.1.6, then verify individual devices cannot be grabbed away from the Plugin Package.
