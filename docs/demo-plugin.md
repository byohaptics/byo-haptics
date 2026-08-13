# Demo Output Plugin

Document version: `0.1.6`

## Identity

- Plugin ID: `io.github.byohaptics.output.demo`
- Plugin version: `0.1.4`
- Contract: `BYOHaptics.Output.v1`
- Transport: direct scene fields
- Connection reporting: available

## Package Contents And Connection

The Plugin Package contains two simulated devices named `Demo Device Left` and `Demo Device Right`. They remain children of the plugin root and are connected through direct Slot field references generated from the same SlotSpec. The demo does not use OSC, dynamic-variable discovery, pairing, or global scene lookup.

Each device visual uses one Canvas Slot with separate child Slots for its background and label. A RectTransform is not added to the Canvas Slot itself.

Keeping the devices inside the package prevents one copied demo plugin from driving another copy's devices. Installing the plugin under a Host moves the complete demo together.

The package card follows the Joy-Con Plugin visual language without copying its configuration-sized height. It uses a `420 × 186` rounded panel, opaque backing, an 18-pixel accent stripe, and the same typography. Its four metadata rows total 150 pixels with 24 pixels of top padding and 12 pixels of bottom padding, so no flexible vertical gap remains. The accent stripe is purple so it is distinct from the Joy-Con blue and Haptira orange stripes.

The simulated devices use base positions `(-0.55, 0, 0)` and `(0.55, 0, 0)` relative to the plugin root. When the plugin is dropped into the Host socket, they sit outside the Host UI panel instead of overlapping it.

## Target Routing

- Target `left` drives `Demo Device Left`.
- Target `right` drives `Demo Device Right`.
- Other Target values are ignored.

For each row, combine Force, Vibration, Pain, and Temperature by maximum value. For each target, use the maximum intensity of all rows assigned to it.

## Simulated Vibration

Each device uses a `Wiggler` component targeting its Slot `Rotation` field. This represents a vibration motor as small, fast angular motion; horizontal translation is not used because contact friction constrains it more strongly than rotation.

Wiggle speed is fixed. The normalized target intensity drives angular magnitude up to thirty degrees on each axis and enables the component only above zero. At zero intensity the device returns to its identity base rotation. The exaggerated range is intentional: these are visual indicators for a demo, not a physical vibration simulation.

The devices expose their current normalized intensity for inspection. The plugin reports connected while it is selected, contract-compatible, active, and installed because both devices are package-owned direct references.

## Acceptance

1. Drop the Demo Output Plugin card into BYO Haptics.
2. Assign sources to two rows and set their Targets to `left` and `right`.
3. Touch each sampler source to an item with `HapticVolume`.
4. Confirm only the matching simulated device vibrates rotationally and returns to its base rotation when the sensation stops.
