# Joy-Con Bridge

Document version: `0.1.0`

## Identity

- Bridge version: `0.1.0`
- Bridge API version: `0.1.0`
- Transport: OSC over UDP to Bluetooth HID
- Supported host plugin: `io.github.byohaptics.output.joycon.osc`

## Wire Protocol

The Bridge listens on `127.0.0.1:9010` by default. Ports `9000` and `9001` are avoided because they are commonly used by face-tracking applications. `<target>` must exactly match a configured `devices[].osc_address`.

```text
/avatar/parameters/<namespace>/status/port                  int32
/avatar/parameters/<namespace>/heartbeat                    int32
/avatar/parameters/<namespace>/channel/<target>/force       float32
/avatar/parameters/<namespace>/channel/<target>/vibration   float32
/avatar/parameters/<namespace>/channel/<target>/pain        float32
/avatar/parameters/<namespace>/status/heartbeat             int32
```

The first five messages travel from Plugin to Bridge. The final acknowledgement travels from Bridge to the sender IP and registered status port. Sensation values are finite `float32` values clamped to `0..1`.

## Liveness

The sender registers its acknowledgement port and emits a heartbeat about every `500 ms`. The Bridge echoes each valid heartbeat sequence. If no heartbeat arrives for `2000 ms`, the Bridge clears all sensation state and sends stop reports to both controllers.

## Output Scheduling

OSC receive processing keeps only the latest requested values. The sensation engine calculates a frame every `50 ms`; HID output refresh runs every `15 ms`. Slow writes therefore do not build an unbounded queue of stale rumble commands. A transition to zero sends an immediate stop report.

## Sensation Mapping

- Force maps value linearly to amplitude and raises requested frequency from `20 Hz` to `160 Hz`.
- Vibration reaches full requested amplitude at value `0.05` and raises requested frequency from `5 Hz` to `320 Hz`.
- Pain uses a pulsed envelope and bounded random variation.
- Simultaneous sensations are blended by their current values.
- Temperature is unsupported in Bridge API `0.1.0`.

Frequency carriers may be replaced by a per-controller IMU profile. Without a profile, built-in carrier bands are used.

## Configuration

`joycon-rumble.toml` stores the listen address, namespace, timeout, frequency scale, profile path, and two controller bindings. Each binding contains side, Bluetooth address selection, OSC Target name, and player ID. The default `bluetooth_address = "auto"` selects the first connected controller of the configured side. Set a 12-digit Bluetooth address only when multiple controllers of the same side must be distinguished. Precedence is command line, configuration file, then built-in defaults.

Use `--device SIDE,AUTO_OR_BLUETOOTH_ADDRESS,OSC_ADDRESS,ID` to set a binding and `--save-config` to persist effective values.

## Utilities

- `joycon-list`: list paired/openable controllers.
- `joycon-rumble-test`: send a short direct hardware test.
- `joycon-imu-sweep`: measure candidate carriers and save an optimized profile.
- `--dry-run`: parse and route without accessing HID devices.
- `--trace-csv`: record receive, frame, HID, and optional IMU timing for diagnostics.
