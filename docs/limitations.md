# Known Limitations

Document version: `0.2.0`

## Multi-user Execution

- A Host is locally active only on the client whose local User Root contains it. The same synchronized Host remains inactive on other clients.
- Device transports run per client. Installing a Host does not send output through another user's computer or device service.
- A visitor must install their own Host and configure their own sources and output plugin.
- Multi-user correctness cannot be established by inspecting one client alone; each participating client must verify its own local output.

## Output Plugins

- One Host accepts one active Output Plugin Package at a time. Installing another package ejects the current package.
- Target strings are plugin-defined. Switching plugins can require changing row Targets.
- Haptira device discovery is not implemented. Its plugin requires a manually configured device address.
- Haptira does not provide a positive connection acknowledgement, so its connection state is reported as unknown rather than connected.
- The Joy-Con plugin requires the local Bridge acknowledgement before non-zero output is sent.
- A ProtoFlux `UpdatesTimer` interval counts engine updates, not seconds, so its real period follows frame rate. Host state that a user observes must be driven from `LocalUpdate`; a timer may only carry periodic maintenance.

## Current verification limits

- Physical Source drag/drop, BodyNode picker and row-control gestures, two-user regression and inventory save/reload acceptance remain incomplete for the current revision.
- Force has been observed to persist without reported contact; its cause remains unresolved.
- Weak transient inputs may produce weak device vibration. Default row gains remain 1; no automatic input-sensitivity adjustment is provided.
- Static validation and compilation do not establish live VR or physical device behavior.
- The revised OSC shutdown/route-change timers and staged replacement workflow
  have offline regression tests; live UDP capture, physical stop behavior and
  interrupted scene replacement still require acceptance testing.

## Runtime And Distribution

- Runtime IDs and ResoniteLink endpoints are session-specific and are regenerated during deployment.
- Source builds do not produce an inventory item without a live Resonite deployment and save operation.
- The separately maintained [Joy-Con Bridge](https://github.com/byohaptics/joycon-bridge) is currently packaged for Windows x64.
- FluxSDK `1.9.0` can compile read-only collection operations when the collection type is inferred, but the explicit ProtoGraph type name `IReadOnlyList<Slot>` remains unresolved. Use inferred generic parameters such as `ReadOnlyCount<_,Slot>` and require a zero-error compiler diagnostic set.
