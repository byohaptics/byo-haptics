# Architecture

Document version: `0.2.0`

## Data Flow

```text
HapticVolume
  -> VirtualHapticPointSampler x 4
  -> normalized Output Bus rows
  -> selected Output Plugin
  -> device service or device
```

The host owns sampling, row validity, gain, lifecycle, source binding, and UI. A plugin owns Target interpretation, sensation mapping, transport, device configuration, and connection reporting.

The Joy-Con OSC plugin sends normalized values and liveness messages to the separately maintained [Joy-Con Bridge](https://github.com/byohaptics/joycon-bridge) through the [Bridge API contract](joycon-bridge-contract.md). The Bridge owns controller discovery, optional Bluetooth address binding, sensation-to-rumble conversion, fixed-rate HID output, reconnection, and diagnostics. Its application version is independent from the Plugin Package and Bridge API versions.

The Demo plugin contains its `left` and `right` simulated devices and drives their Slot fields directly. It adds no external transport or discovery path.

## Host Modules

The host ProtoGraph is divided into six operational sheets:

1. `BYOHapticsLifecycle`
2. `BYOHapticsPositioning`
3. `BYOHapticsSampler`
4. `BYOHapticsPluginDiscovery`
5. `BYOHapticsPluginPackageManager`
6. `BYOHapticsOutputBus`

Reusable avatar-root and panel-reset modules live in `Common/`. The deployment
sheets import them through project-relative paths such as
`use Common/PanelResetFromUser`.

Read-only diagnostic mirror fields are omitted. Inspect the authoritative sampler and Output Bus fields directly when debugging.

Host-internal state is grouped by ownership rather than giving every field its own Slot. Publicly adjusted settings retain named children under `Config`; row bookkeeping shares `Config/State`, Output Plugin contract variables share the Bus Slot, and the three plugin-state indicators share `Diagnostics/Plugin`. Component aliases and Dynamic Variable names remain the stable references.

## Ownership And Activity

Local-user containment determines which client runs the Host. The
[lifecycle state table](lifecycle.md#derived-state) defines Host activity and
write ownership; [sampler isolation](sampler.md#local-user-isolation) defines
the per-user subtree implementation. Plugin runtime follows the separate
[activation contract](output-plugin-contract.md#plugin-activation).

## Installation

The [lifecycle specification](lifecycle.md) defines installation, uninstallation,
grabbing, Close and panel reset, including parentage and placement rules.

## Source Binding

Each row follows a BodyNode or a manually assigned Slot. The
[sampler specification](sampler.md) defines Source resolution, Active, gain,
pulse retention and position editing. [Host UI](host-ui.md) defines their controls.

## Reproducibility

SlotSpec and ProtoGraph source are authoritative. Deployment uses
[generated IDs](slotspec.md#generated-ids) from the current SlotSpec build.
