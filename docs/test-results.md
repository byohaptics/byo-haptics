# Test Results

Document version: `0.1.0`

The public build was first deployed to a live Resonite world on 2026-08-12. Functional VR and device tests remain pending.

Add one row for each distinct environment and keep failed observations. A later pass does not erase evidence that exposed a missing requirement.

| Date | Build commit | User | World ownership | Component | Scenario | Result | Evidence or notes |
|---|---|---|---|---|---|---|---|
| 2026-08-12 | `c9a25b4` | `byohaptics` | Own world (`byohaptics World`) | Host, Joy-Con OSC Plugin, Haptira OSC Plugin | Deploy all three public SlotSpecs and ProtoGraphs under World Root | Pass | Exactly one new root of each public type was created. Host reported 8 deployed sheets; each plugin reported 1 deployed sheet; module discovery reported 0 errors. |
| 2026-08-12 | `c9a25b4` | `byohaptics` | Own world (`byohaptics World`) | Host and plugin manifests | Read back public version, contract, and Plugin IDs through ResoniteLink | Pass | Host `v0.1.0`; both plugins `v0.1.0`; contract `1`; IDs `io.github.byohaptics.output.joycon.osc` and `io.github.byohaptics.output.haptira.osc`. |

## Required Result Detail

- Host installation state and actual parent slot.
- Node or Slot source mode, selected source, Target, and Visual state.
- Plugin ID, plugin version, transport endpoint, and connection-state presentation.
- Sensation type and observed start/stop behavior.
- For multi-user tests, the observing client and which user's Host was exercised.
- For a failure, reproduction steps and whether the specification or implementation changed.
