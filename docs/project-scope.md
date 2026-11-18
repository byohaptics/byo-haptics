# Project Scope

Document version: `0.2.0`

## Goal

Build a portable Resonite avatar tool that samples four haptic sensation values and routes them through a replaceable output plugin. The initial milestone covers the host, Joy-Con OSC plugin, and Haptira OSC plugin as defined by the public specifications and [verification scenarios](test-plan.md).

## Included

- Four configurable sampler rows.
- Body node and manual slot source modes.
- Force, Vibration, Pain, and Temperature values per row.
- Target strings interpreted only by the selected plugin.
- Per-row visual debug toggle.
- Sampler position editing with avatar-relative offset persistence and reset.
- Install, uninstall, panel reset, panel visibility, and context menu actions.
- Output plugin discovery, replacement, direct card drop, and ejection.
- Joy-Con OSC and Haptira OSC output plugins, plus compatibility with independently maintained Output Contract 2 plugins.
- The BYO Haptic Actuator OSC plugin is owned by the separate `byo-haptic-actuator` repository; this host does not own its channel mapping, versions, tests or release assets.
- A scene-local Demo output plugin with `left` and `right` simulated devices.
- Integration with the externally maintained Joy-Con Bridge through a versioned API contract.
- Reproducible SlotSpec and ProtoGraph build inputs.

## Excluded From The Initial Milestone

- Automatic Haptira device discovery.
- Device-specific mapping inside the host.
- More than one active output plugin per host.
- Compatibility with earlier Output Plugin contracts.

## Identity

- Product display name: `BYO Haptics`
- Root slot name: `BYOHaptics`
- Current product and component versions: [`versions.json`](../versions.json)
- Copyright holder: `byohaptics`
- Contact: `byohaptics@gmail.com`
