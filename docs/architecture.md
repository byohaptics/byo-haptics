# Architecture

Document version: `0.1.0`

## Data Flow

```text
HapticVolume
  -> VirtualHapticPointSampler x 4
  -> normalized Output Bus rows
  -> selected Output Plugin
  -> device service or device
```

The host owns sampling, row validity, gain, lifecycle, source binding, and UI. A plugin owns Target interpretation, sensation mapping, transport, device configuration, and connection reporting.

The Joy-Con OSC plugin sends normalized values and liveness messages to the Joy-Con Rumble Bridge. The Bridge owns controller discovery, Bluetooth address binding, sensation-to-rumble conversion, fixed-rate HID output, reconnection, and diagnostics. It is versioned independently from the Resonite Plugin Package.

## Host Modules

The host ProtoGraph is divided into eight readable sheets:

1. `BYOHapticsLifecycle`
2. `BYOHapticsPositioning`
3. `BYOHapticsSourceBinding`
4. `BYOHapticsSampler`
5. `BYOHapticsPluginDiscovery`
6. `BYOHapticsPluginPackageManager`
7. `BYOHapticsOutputBus`
8. `BYOHapticsDiagnostics`

Diagnostics is read-only and placed below the operational sheets.

## Ownership And Activity

The host is locally active only while its root is under the local user's User Root. This includes an installed host under the avatar and a host currently held in local user space. Samplers are controlled through the `Samplers` slot's per-user active state so another user's client does not sample or emit local output.

## Installation

Install reparents the host below the clicking user's active avatar. Uninstall reparents it to World Root while preserving its world pose. Installed state is derived from actual parentage and is not trusted from a saved boolean alone.

Panel reset places the panel one metre along the local user's Head forward direction. The panel bottom aligns with Head height. The action is disabled while uninstalled.

## Source Binding

Each row supports:

- Node mode: resolve a selected BodyNode on the active avatar.
- Slot mode: use a manually assigned Slot reference.

A null source disables the row and resets its sampler transform and stored offset. Node mode is the default.

## Position Editing

Normal mode follows the source transform plus a stored avatar-relative offset. Edit mode keeps source tracking active while allowing the sampler to be grabbed. Releasing it stores the new offset. Reset clears all stored offsets.

## Reproducibility

SlotSpec and ProtoGraph source are authoritative. Runtime IDs, discovery ports, session IDs, and world-specific references are never source data. Every deployment uses IDs generated from a build in the current session.
