# Joy-Con Bridge API Contract

Document version: `0.1.9`

## Identity And Scope

- Bridge API version: `0.1.0`
- Transport: OSC over UDP
- Plugin: `io.github.byohaptics.output.joycon.osc`
- Default namespace: `joyconrumble`

This contract is the source of truth for communication between the Joy-Con Output Plugin and Joy-Con Bridge. It does not define the BYO Haptics Output Bus, Bridge GUI, Bluetooth discovery, or Joy-Con rumble mapping.

## Endpoint And Paths

The Plugin sends to the configured Bridge address and port, defaulting to `127.0.0.1:9010`. The Bridge derives all paths from its configured namespace. Plugin and Bridge namespaces must match.

| Direction | Path | OSC argument | Meaning |
| --- | --- | --- | --- |
| Plugin to Bridge | `/avatar/parameters/<namespace>/status/port` | one `int32` | Register the acknowledgement UDP port |
| Plugin to Bridge | `/avatar/parameters/<namespace>/heartbeat` | one `int32` | Send a liveness sequence |
| Plugin to Bridge | `/avatar/parameters/<namespace>/channel/<target>/force` | one `float32` | Set the latest Force value |
| Plugin to Bridge | `/avatar/parameters/<namespace>/channel/<target>/vibration` | one `float32` | Set the latest Vibration value |
| Plugin to Bridge | `/avatar/parameters/<namespace>/channel/<target>/pain` | one `float32` | Set the latest Pain value |
| Bridge to Plugin | `/avatar/parameters/<namespace>/status/heartbeat` | one `int32` | Echo the received liveness sequence |

Each message has exactly one argument of the listed type. Unknown paths, unknown Targets, unsupported sensations, extra path segments, wrong argument counts, wrong argument types, and non-finite sensation values are ignored.

## Target And Sensation Values

`<target>` must exactly match a Bridge device `osc_address`. The defaults are `left` and `right`; row numbers are not Targets. Target names use only ASCII letters, digits, `.`, `-`, or `_`.

Force, Vibration, and Pain are supported. Finite values are clamped to `0..1`, and each message replaces the latest value for its Target and sensation. Temperature is unsupported in Bridge API `0.1.0`.

## Registration And Liveness

The Plugin listens for acknowledgements on a separate UDP port, defaulting to `9002`. While locally active and disconnected, it repeatedly sends that port through `status/port`. A valid registration contains a port in `1..65535`; the Bridge associates it with the sender IP.

While active, the Plugin increments and sends a heartbeat sequence every `500 ms`. The Bridge echoes the same sequence to the registered acknowledgement endpoint. The Plugin reports the Bridge connected only while its acknowledgement receiver is active and fewer than four consecutive heartbeat intervals have elapsed without an acknowledgement.

The Bridge considers the Plugin live for `2000 ms` after the most recent valid heartbeat. Sensation messages may update the latest requested state, but the Bridge must not drive controllers without a current heartbeat. When the heartbeat expires, the Bridge clears all sensation state and sends stop reports to both controllers.

UDP delivery is not guaranteed. The timeout behavior is therefore part of the safety contract and must not depend on receiving a final zero message.

## Compatibility

The Joy-Con Plugin and each Bridge release declare the Bridge API versions they support.

- Patch releases clarify the document or fix implementations without changing observable wire behavior.
- Minor releases may add optional paths or behavior while preserving existing messages.
- Breaking changes to paths, argument types, value meaning, acknowledgement behavior, or timeout guarantees require a new incompatible API version.

Bridge API `0.1.0` has no wire-level version negotiation. Add negotiation only when an incompatible protocol version must coexist with `0.1.0`.
