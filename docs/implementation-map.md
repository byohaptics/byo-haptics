# Implementation Map

Document version: `0.1.4`

This map ties each public requirement to its declarative source, executable source, and verification boundary. Specifications remain authoritative when a test exposes a mismatch.

## Host

| Capability | Specification | SlotSpec or graph | Static evidence | Live evidence |
|---|---|---|---|---|
| Install, uninstall, close, visibility, local activity | `lifecycle.md` | `byo-haptics.resoslots.json`, `BYOHapticsLifecycle.pg` | SlotSpec validation and graph compilation | Pending |
| Head-relative panel reset | `lifecycle.md`, `host-ui.md` | `BYOHapticsPositioning.pg`, `PanelResetFromUser.pg` | Graph compilation | Pending |
| Node and manual Slot source modes | `sampler.md` | `BYOHapticsSourceBinding.pg` | Default and reference validation | Pending |
| Four sensation samplers and null-source disable | `sampler.md` | `BYOHapticsSampler.pg` | Four-row manifest validation and graph compilation | Pending |
| Avatar-relative edit offsets and reset | `sampler.md` | `BYOHapticsSampler.pg` | Graph compilation | Pending |
| Output Plugin discovery | `output-plugin-contract.md` | `BYOHapticsPluginDiscovery.pg` | Contract and Plugin ID validation | Pending |
| Package drop, replacement, and ejection | `output-plugin-package.md` | `BYOHapticsPluginPackageManager.pg` | One `PackageReference` writer path after receipt | Pending |
| Normalized four-row Output Bus | `output-plugin-contract.md` | `BYOHapticsOutputBus.pg` | Contract integer and namespace validation | Pending |
| Read-only diagnostics | `architecture.md` | `BYOHapticsDiagnostics.pg` | Graph compilation | Pending |

## Output Plugins

| Package | Specification | SlotSpec and graph | Static evidence | Live evidence |
|---|---|---|---|---|
| Joy-Con OSC | `joycon-plugin.md` | `joycon-osc-plugin.resoslots.json`, `BYOHapticsJoyConOSCOutput.pg` | 44 slots, 129 components, contract and defaults validated | Row 0/`left` and Row 1/`right` Force passed |
| Haptira OSC | `haptira-plugin.md` | `haptira-osc-plugin.resoslots.json`, `BYOHapticsHaptiraOSCOutput.pg` | 42 slots, 124 components, contract, defaults, and rear backing validated | Channels `00`, `01`, `02`, and `15` passed |
| Demo | `demo-plugin.md` | `demo-output-plugin.resoslots.json`, `BYOHapticsDemoOutput.pg` | 26 slots, 58 components, Joy-Con-style card, distinct accent, routing, Wiggler targets, and device offsets validated | Routing and response passed; visual polish deployed and awaiting acceptance |

Both graphs consume the same transport-neutral Output Bus. Row Targets remain opaque to the Host and are interpreted only by the selected Plugin.

## Joy-Con Bridge

| Capability | Specification | Source | Static evidence | Hardware evidence |
|---|---|---|---|---|
| OSC parsing and Target routing | `joycon-bridge.md` | `osc.rs`, `signal.rs`, `transport.rs` | Unit tests | Pending public build |
| Heartbeat acknowledgement and timeout stop | `joycon-bridge.md` | `main.rs`, `signal.rs` | Unit tests | Pending public build |
| Force, Vibration, and Pain mapping | `joycon-bridge.md` | `sensation.rs` | Unit tests | Pending public build |
| Fixed-rate newest-state HID output | `joycon-bridge.md` | `main.rs`, `joycon.rs` | Scheduler and coalescing tests | Pending public build |
| Device configuration and reconnect | `joycon-bridge.md` | `config.rs`, `joycon.rs` | Configuration tests and Clippy | Pending public build |
| CLI, GUI, trace, and IMU calibration | `building.md`, Bridge README | `main.rs`, `bin/gui.rs`, `trace.rs`, `joycon.rs` | All-target build and 32 tests | Passed live device tests |

## Deliberate Public Differences

- Product, document, package, Bridge, and Bridge API versions restart at `0.1.0`.
- Runtime Output contract restarts at integer `1` and namespace `BYOHaptics.Output.v1`.
- Public Plugin IDs use the `io.github.byohaptics` namespace.
- A previously unused Plugin error field and indicator are omitted; presence and connection state are the supported status model.
- Compatibility paths for superseded private contracts are omitted.
- Personal device addresses, session IDs, generated scene artifacts, logs, traces, and non-public output integrations are omitted.

The remaining parity proof is live execution under the public account. Results belong in `test-results.md`.
