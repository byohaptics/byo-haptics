# Demo Output Plugin

Document version: `0.1.2`

## Identity

- Plugin ID: `io.github.byohaptics.output.demo`
- Plugin version: `0.1.0`
- Contract: `BYOHaptics.Output.v1`
- Transport: direct scene fields
- Connection reporting: available

## Package Contents And Connection

The Plugin Package contains two simulated devices named `Demo Device Left` and `Demo Device Right`. They remain children of the plugin root and are connected through direct Slot field references generated from the same SlotSpec. The demo does not use OSC, dynamic-variable discovery, pairing, or global scene lookup.

Each device visual uses one Canvas Slot with separate child Slots for its background and label. A RectTransform is not added to the Canvas Slot itself.

Keeping the devices inside the package prevents one copied demo plugin from driving another copy's devices. Installing the plugin under a Host moves the complete demo together.

## Target Routing

- Target `left` drives `Demo Device Left`.
- Target `right` drives `Demo Device Right`.
- Other Target values are ignored.

For each row, combine Force, Vibration, Pain, and Temperature by maximum value. For each target, use the maximum intensity of all rows assigned to it.

## Simulated Vibration

While a target intensity is greater than zero, alternate its device Slot `Position` field every local update around a fixed base position. Displacement is intensity multiplied by `0.01` metres. At zero intensity the device returns to its base position.

The devices expose their current normalized intensity for inspection. The plugin reports connected while it is selected, contract-compatible, active, and installed because both devices are package-owned direct references.

## Acceptance

1. Drop the Demo Output Plugin card into BYO Haptics.
2. Assign sources to two rows and set their Targets to `left` and `right`.
3. Touch each sampler source to an item with `HapticVolume`.
4. Confirm only the matching simulated device vibrates and returns to its base position when the sensation stops.
