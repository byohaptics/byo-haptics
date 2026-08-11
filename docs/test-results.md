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

## Required Result Detail

- Host installation state and actual parent slot.
- Node or Slot source mode, selected source, Target, and Visual state.
- Plugin ID, plugin version, transport endpoint, and connection-state presentation.
- Sensation type and observed start/stop behavior.
- For multi-user tests, the observing client and which user's Host was exercised.
- For a failure, reproduction steps and whether the specification or implementation changed.
