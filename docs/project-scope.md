# Project Scope

Document version: `0.1.0`

## Goal

Build a portable Resonite avatar tool that samples four haptic sensation values and routes them through a replaceable output plugin. The initial milestone is complete when the host, Joy-Con OSC plugin, and Haptira OSC plugin reproduce the verified behavior of the private prototype.

## Included

- Four configurable sampler rows.
- Body node and manual slot source modes.
- Force, Vibration, Pain, and Temperature values per row.
- Target strings interpreted only by the selected plugin.
- Per-row visual debug toggle.
- Sampler position editing with avatar-relative offset persistence and reset.
- Install, uninstall, panel reset, panel visibility, and context menu actions.
- Output plugin discovery, replacement, direct card drop, and ejection.
- Joy-Con OSC and Haptira OSC output plugins.
- Joy-Con Rumble Bridge for OSC reception, liveness acknowledgement, sensation mapping, and Bluetooth HID output.
- Reproducible SlotSpec and ProtoGraph build inputs.

## Excluded From The Initial Milestone

- Automatic Haptira device discovery.
- Device-specific mapping inside the host.
- More than one active output plugin per host.
- A compatibility layer for private prototype contracts.
- Publishing inventory artifacts from the private prototype.

## Identity

- Product display name: `BYO Haptics`
- Root slot name: `BYOHaptics`
- Initial product version: `0.1.0`
- Copyright holder: `byohaptica`
- Contact: `byohaptics@gmail.com`
