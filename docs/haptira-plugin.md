# Haptira OSC Plugin

Document version: `0.1.6`

## Identity

- Plugin ID: `io.github.byohaptics.output.haptira.osc`
- Plugin version: `0.1.1`
- Contract: `BYOHaptics.Output.v1`
- Transport: OSC over UDP
- Default device port: `8000`
- Connection reporting: unavailable

## Configuration

The user enters the device IPv4 address manually. Automatic discovery is outside the initial milestone. The plugin derives its sender endpoint from Device Address and Device Port and remains inactive when either value is invalid.

The card uses an opaque back-only backing layer so address and port controls are not visible through its rear face.

## Target Routing

Target must be a two-digit channel from `00` through `15`. Other values are ignored.

For each channel, combine every row targeting that channel. For each row, combine Force, Vibration, Pain, and Temperature by maximum value. Send the maximum row value for that channel to:

```text
/avatar/parameters/haptira/channel/<target>/value <float 0..1>
```

Each channel owns an independent OSC value field. A row must not accidentally write another channel's field.

## Timing

Send changes immediately. Do not enable sender-wide periodic resend because it retransmits all 16 channels and can leave device output active longer than the sampled signal. Inactive state writes zero to every channel and stops the sender.

Because the device protocol has no acknowledgement, `ConnectionStatusAvailable` remains false and the host must not present `Connected=false` as a confirmed disconnection.
