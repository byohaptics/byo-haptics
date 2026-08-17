# Architecture

Document version: `0.1.9`

## Data Flow

```text
HapticVolume
  -> VirtualHapticPointSampler x 4
  -> normalized Output Bus rows
  -> selected Output Plugin
  -> device service or device
```

The host owns sampling, row validity, gain, lifecycle, source binding, and UI. A plugin owns Target interpretation, sensation mapping, transport, device configuration, and connection reporting.

The Joy-Con OSC plugin sends normalized values and liveness messages to the Joy-Con Rumble Bridge. The Bridge owns controller discovery, optional Bluetooth address binding, sensation-to-rumble conversion, fixed-rate HID output, reconnection, and diagnostics. It is versioned independently from the Resonite Plugin Package.

The Demo plugin contains its `left` and `right` simulated devices and drives their Slot fields directly. It adds no external transport or discovery path.

## Host Modules

The host ProtoGraph is divided into six operational sheets:

1. `BYOHapticsLifecycle`
2. `BYOHapticsPositioning`
3. `BYOHapticsSampler`
4. `BYOHapticsPluginDiscovery`
5. `BYOHapticsPluginPackageManager`
6. `BYOHapticsOutputBus`

Read-only diagnostic mirror fields are omitted. Inspect the authoritative sampler and Output Bus fields directly when debugging.

Host-internal state is grouped by ownership rather than giving every field its own Slot. Publicly adjusted settings retain named children under `Config`; row bookkeeping shares `Config/State`, Output Plugin contract variables share the Bus Slot, and the three plugin-state indicators share `Diagnostics/Plugin`. Component aliases and Dynamic Variable names remain the stable references.

## Ownership And Activity

The host is locally active only while its root is under the local user's User Root. This includes an installed host under the avatar and a host currently held in local user space. Samplers are controlled through the `Samplers` slot's per-user active state so another user's client does not sample or emit local output. An always-active Host-root `ValueUserOverride<bool>` drives `Samplers.IsActive`, and ProtoFlux writes that driven field to create the local user's override; the controller does not live on the subtree it can disable.

## Installation

Install reparents the host below the clicking user's active avatar. Uninstall reparents it to World Root while preserving its world pose. Installed state is derived from actual parentage and is not trusted from a saved boolean alone.

Panel reset places the panel one metre along the local user's Head forward direction. The panel bottom aligns with Head height. The action is disabled while uninstalled.

## Source Binding

Each row supports:

- Node mode: resolve a selected BodyNode on the active avatar.
- Slot mode: use a manually assigned Slot reference.

A null source disables the row and resets its sampler transform and stored offset. Node mode is the default. Target routing remains explicitly user-configured; the host does not derive transport-specific Target names from BodyNode selections.

## Position Editing

Normal mode follows the source transform plus a stored avatar-relative offset. Edit mode keeps source tracking active while allowing the sampler to be grabbed. Releasing it stores the new offset. Reset clears all stored offsets.

## Reproducibility

SlotSpec and ProtoGraph source are authoritative. Runtime IDs, discovery ports, session IDs, and world-specific references are never source data. Every deployment uses IDs generated from a build in the current session.
