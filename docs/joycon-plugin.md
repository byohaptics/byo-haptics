# Joy-Con OSC Plugin

Document version: `0.1.0`

## Identity

- Plugin ID: `io.github.byohaptics.output.joycon.osc`
- Plugin version: `0.1.0`
- Bridge API version: `0.1.0`
- Contract: `BYOHaptics.Output.v1`
- Transport: OSC over UDP
- Default send port: `9010`
- Default acknowledgement port: `9002`

## Target Routing

Target is the configured controller address name. Defaults are:

- `left`: left controller, numeric ID `1`
- `right`: right controller, numeric ID `2`

Unknown Target values are not sent. Row number is not a device address.

## Sensation Messages

For each valid row and supported sensation, send:

```text
/avatar/parameters/joyconrumble/channel/<target>/<sensation> <float 0..1>
```

Supported sensation path names are `force`, `vibration`, and `pain`. Temperature is reserved and is not sent in version `0.1.0`.

## Registration And Acknowledgement

When the host becomes locally active, the plugin registers its acknowledgement port with the bridge once per second until a heartbeat is received. It sends sensation values only while heartbeat is current. If heartbeat expires, it stops output, reports disconnected, and resumes registration attempts.

Registration, heartbeat, and acknowledgement use:

```text
/avatar/parameters/joyconrumble/status/port <int32 port>
/avatar/parameters/joyconrumble/heartbeat <int32 sequence>
/avatar/parameters/joyconrumble/status/heartbeat <int32 echoed-sequence>
```

When the host becomes inactive, the plugin stops its receiver and releases the acknowledgement port.

## Bridge Requirements

The bridge uses a fixed-rate output loop. Incoming OSC updates replace the latest desired state; the loop writes only the newest state and does not queue stale HID writes. A final zero update stops output promptly.

Bridge configuration stores listen port, controller address names, numeric IDs, and Bluetooth addresses. Command-line values override the file. Explicit save writes effective command-line values back to the file.
