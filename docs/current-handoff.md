# Current Handoff

Updated: 2026-08-16

## Current State

The history-free public repository foundation exists locally with BYO Haptics at version `0.2.0`. Public architecture, Output Plugin contract, package lifecycle, device plugin behavior, SlotSpec schema, host UI, lifecycle, sampler behavior, development loop, tasks, implementation map, test plan, known limitations, and live-result ledger have initial specifications. `versions.json` records product, document, contract, plugin, Bridge, and Bridge API versions. The SlotSpec builder requires generated IDs and has no runtime-ID fallback. All eight host sheets, both Plugin Package sheets, and shared transform helpers compile with no unresolved modules. Deployment scripts pass F# type checking and require generated IDs plus an explicit deployment gate. Joy-Con and Haptira packages use public IDs, contract v1, version 0.1.1, and validated defaults. Structural parity was audited: Host SlotSpec differs only by the removed unused error indicator; both Plugin Package slot/component structures match; all ten ProtoGraphs match after public names, IDs, and contract version are normalized. The Joy-Con Rumble Bridge CLI and GUI compile at version `0.1.0`; all 32 Rust tests and strict Clippy checks pass. Windows CI installs the pinned FluxSDK and runs the complete static suite. Bridge packaging produces CLI and GUI executables with build paths remapped and rejects remaining private paths. Private history, runtime logs, traces, generated scene artifacts, local paths, and real Bluetooth addresses were not migrated. The publication-policy scan and Git history scan are clean.

The public build was redeployed to `byohaptics World` under Resonite user `byohaptics` on 2026-08-13 after local Resonite data loss. World Root contains one public `BYOHaptics` Host, one Joy-Con OSC Plugin Package, and one Haptira OSC Plugin Package. The Host deployed eight sheets and each plugin deployed one sheet with zero module-discovery errors. Install, uninstall, panel grab, both close behaviors, context-menu and UI panel reset, sampler offset editing, and Reset All Positions passed live tests.

Joy-Con Bridge and Plugin defaults now use `9010/UDP` to avoid common face-tracking ports. Bridge controller bindings default to `bluetooth_address = "auto"`, which selects the first connected controller of each configured side; an explicit address remains available for disambiguation. Live Force tests passed end to end: Row 0/Target `left` drove Joy-Con (L), and Row 1/Target `right` drove Joy-Con (R).

The Joy-Con Plugin Package card exposes configurable Bridge Address and Port fields, defaulting to `127.0.0.1:9010`. The address is passed directly to Resonite's OSC sender without host-name resolution or rewriting. The acknowledgement receiver remains on its separate port. Live device output passed for Force, Vibration, and Pain.

Haptira OSC live tests passed for Targets `00`, `01`, `02`, and upper channel `15`. In a two-user session, both Joy-Con OSC and Haptira OSC drove their devices from Row 0 for both users.

Node source mode passed with Row 3 following BodyNode `RightFoot` and driving Haptira Target `01`. Slot source mode passed for Rows 0 through 3 with assigned Source slots. Null-source disable passed for all four rows with Node set to `NONE`, and Slot mode remained inactive with Source null. Plugin replacement automatically installed the dropped plugin and ejected the previous plugin. Inventory save, spawning and installation in another world passed. Installation and device output also passed in another user's world. Stopping the Joy-Con Bridge changed the Link indicator to down; restarting the Bridge restored both the connected indication and device output without reinsertion. Phase 6 live verification is complete. The private GitHub repository exists under `byohaptics`, with `main` and `v0.1.0` pushed.

Both Plugin Package SlotSpecs now include the same `License` policy as the Host: credit required, credit string `byohaptics`, and export disabled. The source change requires rebuilding and redeploying both Plugin Packages before the live objects contain these components.

The Demo Output Plugin is implemented at version `0.1.5`. The user passed direct drop, two-row `left`/`right` routing, and HapticVolume response in `byohaptics World`, but a forced 0.85 signal showed that the original Wiggler target produced no visible motion despite correct Intensity, Enabled, and magnitude values. The Wiggler now targets the dedicated `Devices/<side>/Visual.Rotation` child field instead of the interactive device root Rotation, eliminating the display-writer conflict while retaining the thirty-degree maximum. Its card remains content-sized at 420 by 186 pixels, and the device roots remain at x = -0.55 m and +0.55 m. The 26-slot, 58-component SlotSpec validates, all three Plugin graphs compile, and v0.1.5 was redeployed to `byohaptics World` with 408 responses, zero input-reference corrections, and zero module-discovery errors. Live visible-motion confirmation remains pending.

Sampler Edit uses stable labels `0` through `3`, always exposes all four rows, and each Host UI row starts with `0:` through `3:`. The user confirmed the row-index position, all four Sampler Edit items, and Row 0 selection behavior. The row-index font is now 32 px in a fixed 60 px column, and the header has a matching blank 60 px column so Node and later headers align with their controls. The updated Host was rebuilt and redeployed to `byohaptics World`; live inspection confirmed the new width and font values. Visual acceptance of the enlarged index and checkbox clearance is pending.

The Output Bus retains each positive Force, Vibration, Pain, and Temperature value independently per row for at least `Config/MinimumPulseDuration`. The default is `0.08` seconds, zero disables retention, a new positive sample replaces the retained value and restarts its duration, and an invalid Host, row, Source, or Target still clears output immediately. The user confirmed in VR that short collisions feel effective. A Ponytail review then removed the redundant held-value fields by reusing the mutable Output Bus values; this reduced the Host by 16 components and the Output Bus graph from 1589 to 1429 responses. The simplified Host was deployed to `byohaptics World` with zero module-discovery errors, and all static tests pass.

## Next Task

Verify the enlarged row indices, Node-checkbox clearance, and header alignment in VR.

## Blocking Decisions

- User VR confirmation of visible Demo device motion.

Neither decision blocks local specification and reconstruction work.
