# BYO Haptics v0.4.0

This development revision updates the Host Source controls. See the
[known limitations](limitations.md) for the current verification boundaries.

## Host changes

- Replace the Node mode checkbox with Active. Inactive rows preserve Source
  display, selection and offsets while disabling sampling and clearing held output.
- Default to BodyNode selection. Dropping a Slot on Source selects Slot tracking
  automatically. Clear returns to BodyNode/NONE and resets the sampler offset.
- Show previous/next arrows only in BodyNode mode, restoring them after clear.
- Preserve the Output Contract 2 namespace and independent 80 ms sensation retention.
- Retain default row gains of 1.

## External plugins

The BYO Haptic Actuator plugin and its device behavior are maintained in the
separate `byo-haptic-actuator` repository. The Host uses the common Output
Contract without device-specific mapping.

## Versions

- Host/product: `0.4.0`.
- Output Plugin Contract: `2` (unchanged).
- Joy-Con OSC, Haptira OSC and Demo plugins: `0.2.0` (independent).
- BHA plugin: independently versioned in `byo-haptic-actuator`.
- Documents: `0.2.0`; Joy-Con Bridge API: `0.1.0` (independent).

## Verification and remaining work

Source/UI routing passed static checks, compilation and isolated four-row live
readback. Physical UI gestures, two-user regression and inventory save/reload acceptance remain
unverified. Non-contact Force persistence remains unresolved. Weak transient
inputs may produce weak vibration; a permanent input-sensitivity design has
not been accepted. See [known limitations](limitations.md).

This update does not distribute scene inventories, runtime IDs or device images.
