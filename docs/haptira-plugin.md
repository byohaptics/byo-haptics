# Haptira OSC Plugin

Document version: `0.2.0`

## Identity

- Plugin ID: `io.github.byohaptics.output.haptira.osc`
- Plugin version: `0.2.0`
- Contract: `BYOHaptics.Output.v2`
- Transport: OSC over UDP
- Default device port: `8000`
- Connection reporting: unavailable

## Configuration

The user enters the device IPv4 address manually. Automatic discovery is outside the initial milestone. The plugin derives its sender endpoint from Device Address and Device Port and remains inactive when either value is invalid.

Changing the address, port or channel path stops output on the old route first.
Keep that route unchanged during its 250 ms zero-only tail, then apply the latest
valid configuration. No new Bus sensations are routed during the transition.

The card uses an opaque back-only backing layer so address and port controls are not visible through its rear face.

## Target Routing

Target must be a two-digit channel from `00` through `15`. Other values are ignored.

For each channel, combine every row targeting that channel. For each row, combine Force, Vibration, Pain, and Temperature by maximum value. Send the maximum row value for that channel to:

```text
/avatar/parameters/haptira/channel/<target>/value <float 0..1>
```

Each channel owns an independent OSC value field. A row must not accidentally write another channel's field.

## Timing

Send changes immediately. While any channel is active and for `250 ms` after the last positive output, resend the current state of all 16 channels every `50 ms`. This bounded tail repeats the final zero state because the device protocol uses UDP without acknowledgement. Outside that window, disable periodic resend.

Deactivation zeroes every channel and follows the [common OSC shutdown rules](output-plugin-contract.md#plugin-activation).

Because the device protocol has no acknowledgement, Manifest `CanReportConnection` remains false and the host must not present `Connected=false` as a confirmed disconnection.

Both shutdown and post-pulse timers retain client-local state across updates.
The post-pulse window is continuous even for pulses shorter than 250 ms; a
delayed copy of the pulse is not a hold timer. Channel output is exactly zero
when there is no sensation.
