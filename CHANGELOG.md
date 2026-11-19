# Changelog

## v0.4.0

This development revision updates the Host Source controls. See the
[known limitations](docs/limitations.md) for the current verification boundaries.

### Host changes

- Replace the Node mode checkbox with Active. Inactive rows preserve Source
  display, selection and offsets while disabling sampling and clearing held output.
- Default to BodyNode selection. Dropping a Slot on Source selects Slot tracking
  automatically. Clear returns to BodyNode/NONE and resets the sampler offset.
- Show previous/next arrows only in BodyNode mode, restoring them after clear.
- Preserve the Output Contract 2 namespace and independent 80 ms sensation retention.
- Retain default row gains of 1.

### External plugins

The BYO Haptic Actuator plugin and its device behavior are maintained in the
separate `byo-haptic-actuator` repository. The Host uses the common Output
Contract without device-specific mapping.

### Versions

- Host/product: `0.4.0`.
- Output Plugin Contract: `2` (unchanged).
- Joy-Con OSC, Haptira OSC and Demo plugins: `0.2.0` (independent).
- BHA plugin: independently versioned in `byo-haptic-actuator`.
- Documents: `0.2.0`; Joy-Con Bridge API: `0.1.0` (independent).

### Verification and remaining work

Source/UI routing passed static checks, compilation and isolated four-row live
readback. Physical UI gestures, two-user regression and inventory save/reload acceptance remain
unverified. Non-contact Force persistence remains unresolved. Weak transient
inputs may produce weak vibration; a permanent input-sensitivity design has
not been accepted. See [known limitations](docs/limitations.md).

This update does not distribute scene inventories, runtime IDs or device images.

## v0.3.0

BYO Haptics v0.3.0 introduces Output Plugin Contract 2 and improves plugin lifecycle handling.

### Breaking change: Output Contract 2

- The Bus now uses the `BYOHaptics.Output.v2` namespace.
- `Rows` references a list of `float4` sensation vectors; `Targets` references a same-length string list.
- Each vector remains Force / Vibration / Pain / Temperature. The current Host provides four rows, while the contract and plugin readers support variable row counts.
- The public Bus has four variables: `Rows`, `Targets`, `ActivePluginId` and `ConnectionState`.
- Missing, empty or mismatched lists disable output. Per-row, per-sensation minimum pulse retention remains independent.

**Update the Host and plugins together. Contract 1 plugins are not compatible with this Host.** Existing scene and inventory objects are not updated automatically. Third-party authors must migrate their Bus bindings; the authoring guide and minimal template have been updated.

### Fixes and improvements

- Restrict installed runtime and transport writes to the owning local client.
- Control OSC senders through `Enabled` rather than writing Resonite's `IsSending` status. Deactivation clears sensations, sends a bounded zero-only tail, then disables the sender.
- Update plugin card visibility independently of transport ownership, including when a plugin is inserted into a Host at World Root.
- Show `Contract: 2` in the Host title.
- Improve connection indicators and clip plugin card accents to their rounded outlines.
- Add SlotSpec float4 lists and indexed field references; use FluxSDK 1.9.0.

### Component versions

- Host: `0.3.0`
- Output Plugin Contract: `2`
- Joy-Con OSC, Haptira OSC and Demo plugins: `0.2.0`
- Joy-Con Bridge API: `0.1.0` (unchanged)

### Verification and limitations

Static tests, graph compilation and clean deployment/readback passed. Final World Root drop/eject visual acceptance, two-client regression and some device-level Contract 2 tests remain pending. See [known limitations](docs/limitations.md) for current acceptance boundaries.

One Host accepts one active plugin. Multiple installed Hosts can independently drive different devices. Haptira requires a manually configured address and has no positive connection acknowledgement. A bounded UDP zero resend reduces loss risk but cannot guarantee packet delivery.

### Distribution

This release contains reproducible SlotSpec, ProtoGraph, documentation and build tooling, not Resonite inventory exports or session-specific artifacts. The Joy-Con Bridge application is distributed separately from [byohaptics/joycon-bridge](https://github.com/byohaptics/joycon-bridge).
