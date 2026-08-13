# Current Handoff

Updated: 2026-08-13

## Current State

The history-free public repository foundation exists locally at version `0.1.0`. Public architecture, Output Plugin contract, package lifecycle, device plugin behavior, SlotSpec schema, host UI, lifecycle, sampler behavior, development loop, tasks, implementation map, test plan, known limitations, and live-result ledger have initial specifications. `versions.json` records product, document, contract, plugin, Bridge, and Bridge API versions. The SlotSpec builder requires generated IDs and has no runtime-ID fallback. All eight host sheets, both Plugin Package sheets, and shared transform helpers compile with no unresolved modules. Deployment scripts pass F# type checking and require generated IDs plus an explicit deployment gate. Joy-Con and Haptira packages use public IDs, contract v1, version 0.1.0, and validated defaults. Structural parity was audited: Host SlotSpec differs only by the removed unused error indicator; both Plugin Package slot/component structures match; all ten ProtoGraphs match after public names, IDs, and contract version are normalized. The Joy-Con Rumble Bridge CLI and GUI compile at version `0.1.0`; all 31 Rust tests and strict Clippy checks pass. Windows CI installs the pinned FluxSDK and runs the complete static suite. Bridge packaging produces CLI and GUI executables with build paths remapped and rejects remaining private paths. Private history, runtime logs, traces, generated scene artifacts, local paths, and real Bluetooth addresses were not migrated. The publication-policy scan and Git history scan are clean. `ponytail-review` found one dead dry-run branch, which was removed; no accepted finding remains.

The public build was redeployed to `byohaptics World` under Resonite user `byohaptics` on 2026-08-13 after local Resonite data loss. World Root contains one public `BYOHaptics` Host, one Joy-Con OSC Plugin Package, and one Haptira OSC Plugin Package. The Host deployed eight sheets and each plugin deployed one sheet with zero module-discovery errors. Install, uninstall, panel grab, both close behaviors, context-menu and UI panel reset, sampler offset editing, and Reset All Positions passed live tests.

Joy-Con Bridge and Plugin defaults now use `9010/UDP` to avoid common face-tracking ports. Bridge controller bindings default to `bluetooth_address = "auto"`, which selects the first connected controller of each configured side; an explicit address remains available for disambiguation. Live Force tests passed end to end: Row 0/Target `left` drove Joy-Con (L), and Row 1/Target `right` drove Joy-Con (R).

The Joy-Con Plugin Package card exposes configurable Bridge Address and Port fields, defaulting to `127.0.0.1:9010`. The address is passed directly to Resonite's OSC sender without host-name resolution or rewriting. The acknowledgement receiver remains on its separate port.

Haptira OSC live tests also passed for Row 0/Target `00`, Row 1/Target `01`, and Row 2/Target `02`. The planned upper-channel case remains untested. In a two-user session, both Joy-Con OSC and Haptira OSC drove their devices from Row 0 for both users.

Remaining live coverage includes source-mode/null-source behavior on the public rebuild, complete Joy-Con sensation and disconnect/reconnect cases, an upper Haptira channel, plugin replacement/ejection and inventory/world transitions, and another user's world. No GitHub repository has been created or pushed.

## Next Task

Run the remaining Phase 6 functional VR tests against the deployed public Host and Plugin Packages. Create the GitHub repository when the account holder is ready.

## Blocking Decisions

- GitHub repository creation under the `byohaptics` organization.

Neither decision blocks local specification and reconstruction work.
