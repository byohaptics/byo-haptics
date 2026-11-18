# BYO Haptics v0.3.0

BYO Haptics v0.3.0 introduces Output Plugin Contract 2 and improves plugin lifecycle handling.

## Breaking change: Output Contract 2

- The Bus now uses the `BYOHaptics.Output.v2` namespace.
- `Rows` references a list of `float4` sensation vectors; `Targets` references a same-length string list.
- Each vector remains Force / Vibration / Pain / Temperature. The current Host provides four rows, while the contract and plugin readers support variable row counts.
- The public Bus has four variables: `Rows`, `Targets`, `ActivePluginId` and `ConnectionState`.
- Missing, empty or mismatched lists disable output. Per-row, per-sensation minimum pulse retention remains independent.

**Update the Host and plugins together. Contract 1 plugins are not compatible with this Host.** Existing scene and inventory objects are not updated automatically. Third-party authors must migrate their Bus bindings; the authoring guide and minimal template have been updated.

## Fixes and improvements

- Restrict installed runtime and transport writes to the owning local client.
- Control OSC senders through `Enabled` rather than writing Resonite's `IsSending` status. Deactivation clears sensations, sends a bounded zero-only tail, then disables the sender.
- Update plugin card visibility independently of transport ownership, including when a plugin is inserted into a Host at World Root.
- Show `Contract: 2` in the Host title.
- Improve connection indicators and clip plugin card accents to their rounded outlines.
- Add SlotSpec float4 lists and indexed field references; use FluxSDK 1.9.0.

## Component versions

- Host: `0.3.0`
- Output Plugin Contract: `2`
- Joy-Con OSC, Haptira OSC and Demo plugins: `0.2.0`
- Joy-Con Bridge API: `0.1.0` (unchanged)

## Verification and limitations

Static tests, graph compilation and clean deployment/readback passed. Final World Root drop/eject visual acceptance, two-client regression and some device-level Contract 2 tests remain pending. See [known limitations](limitations.md) for current acceptance boundaries.

One Host accepts one active plugin. Multiple installed Hosts can independently drive different devices. Haptira requires a manually configured address and has no positive connection acknowledgement. A bounded UDP zero resend reduces loss risk but cannot guarantee packet delivery.

## Distribution

This release contains reproducible SlotSpec, ProtoGraph, documentation and build tooling, not Resonite inventory exports or session-specific artifacts. The Joy-Con Bridge application is distributed separately from [byohaptics/joycon-bridge](https://github.com/byohaptics/joycon-bridge).
