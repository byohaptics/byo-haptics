# Test Results

Document version: `0.1.0`

The public build was first deployed to a live Resonite world on 2026-08-12. Functional VR and device tests remain pending.

Add one row for each distinct environment and keep failed observations. A later pass does not erase evidence that exposed a missing requirement.

| Date | Build commit | User | World ownership | Component | Scenario | Result | Evidence or notes |
|---|---|---|---|---|---|---|---|
| 2026-08-12 | `c9a25b4` | `byohaptics` | Own world (`byohaptics World`) | Host, Joy-Con OSC Plugin, Haptira OSC Plugin | Deploy all three public SlotSpecs and ProtoGraphs under World Root | Pass | Exactly one new root of each public type was created. Host reported 8 deployed sheets; each plugin reported 1 deployed sheet; module discovery reported 0 errors. |
| 2026-08-12 | `c9a25b4` | `byohaptics` | Own world (`byohaptics World`) | Host and plugin manifests | Read back public version, contract, and Plugin IDs through ResoniteLink | Pass | Host `v0.1.0`; both plugins `v0.1.0`; contract `1`; IDs `io.github.byohaptics.output.joycon.osc` and `io.github.byohaptics.output.haptira.osc`. |
| 2026-08-12 | `f7da531` | `byohaptics` | Own world (`byohaptics World`) | Joy-Con Bridge | Row 0 Force sampler reacts but paired Joy-Con does not | Fail | OSC endpoint, heartbeat, plugin Active, plugin Connected, Target `left`, and HID openability were valid. Public defaults still filtered devices through sanitized placeholder Bluetooth addresses. |
| 2026-08-12 | working tree after `f7da531` | `byohaptics` | Own world (`byohaptics World`) | Joy-Con Bridge | Start Bridge with automatic side binding | Pass | Updated Bridge connected both paired controllers as `left`/ID 1 and `right`/ID 2 on `127.0.0.1:9010`. End-to-end rumble retest remains pending. |
| 2026-08-12 | `d916a3d` | `byohaptics` | Own world (`byohaptics World`) | Joy-Con Plugin and Bridge | Route Force from Row 0/Target `left` and Row 1/Target `right` | Pass | Row 0 drove Joy-Con (L), and Row 1 drove Joy-Con (R). This confirms end-to-end sampler, Output Bus, OSC, automatic side binding, and HID output for Force. |
| 2026-08-12 | `1a8d17a` | `byohaptics` | Own world (`byohaptics World`) | Haptira OSC Plugin | Route sampler output from Row 0/Target `00`, Row 1/Target `01`, and Row 2/Target `02` | Pass | Haptira channels `00`, `01`, and `02` each drove the corresponding device output. An upper-channel test remains pending. |
| 2026-08-12 | `1889ebb` | `byohaptics` | Own world (`byohaptics World`) | Haptira OSC Plugin card | Inspect the back of the card after adding an opaque back-only backing | Pass | Address and port controls no longer show through the back of the card. |
| 2026-08-12 | working tree after `54a2632` | `byohaptics` | Own world (`byohaptics World`) | Joy-Con OSC Plugin card | Change Bridge Port from `9010` to `9011`, then restore it | Pass | Both Config Endpoint and OSC Sender URL followed `osc://localhost:9011` and returned to `osc://localhost:9010`. Address and Port editors referenced their intended Config fields. |
| 2026-08-12 | working tree after `67e8a1a` | `byohaptics` | Own world (`byohaptics World`) | Host lifecycle and positioning | Install, uninstall, grab the UI panel, use context-menu Panel Position Reset, and use UI Panel Position Reset | Pass | UI reset began working after its transform update moved from button `Pressed` to `Released`. The ongoing interaction with the Grabbable panel had overwritten the transform written during `Pressed`. |
| 2026-08-12 | working tree after `67e8a1a` | `byohaptics` | Own world (`byohaptics World`) | Host close button | Close the panel while installed, then close a Root-level uninstalled Host | Pass | Installed close hid the panel. Uninstalled close destroyed the Host under World Root. |
| 2026-08-12 | working tree after `67e8a1a` | `byohaptics` | Own world (`byohaptics World`) | Joy-Con OSC Plugin and Bridge | Insert the plugin, establish acknowledgement link status, and drive Joy-Con (L) | Pass | Plugin insertion succeeded, the Link indicator reported connected, and sampler output drove Joy-Con (L) after using numeric loopback. The temporary `localhost` rewrite was subsequently removed and the configured default changed to `127.0.0.1` to avoid implying general host-name resolution. |
| 2026-08-12 | working tree after `67e8a1a` | `byohaptics` | Own world (`byohaptics World`) | Joy-Con OSC Plugin and Bridge | Use the direct `127.0.0.1:9010` default with Row 0 targeting `left` | Pass | Row 0 drove Joy-Con (L) normally with no `localhost` rewrite in the plugin. |
| 2026-08-13 | working tree after `67e8a1a` | `byohaptics` | Own world (`byohaptics World`) | Sampler Edit | Open Sampler Edit from the context menu, adjust the Row 0 sampler position offset, then use Reset All Positions | Pass | Row 0 retained the adjusted position offset, and Reset All Positions cleared the sampler position offsets. |
| 2026-08-13 | working tree after `67e8a1a` | Two users | Multi-user session | Joy-Con OSC and Haptira OSC Plugins | Exercise Row 0 for both plugins from each user's installed BYO Haptics | Pass | In the two-user environment, each plugin drove its device from Row 0 for both users. This confirms reciprocal per-user operation in the tested session. |

## Required Result Detail

- Host installation state and actual parent slot.
- Node or Slot source mode, selected source, Target, and Visual state.
- Plugin ID, plugin version, transport endpoint, and connection-state presentation.
- Sensation type and observed start/stop behavior.
- For multi-user tests, the observing client and which user's Host was exercised.
- For a failure, reproduction steps and whether the specification or implementation changed.
