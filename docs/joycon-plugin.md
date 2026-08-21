# Joy-Con OSC Plugin

Document version: `0.1.9`

## Identity

- Plugin ID: `io.github.byohaptics.output.joycon.osc`
- Plugin version: `0.1.1`
- Bridge API version: `0.1.0`
- Contract: `BYOHaptics.Output.v1`
- Transport: OSC over UDP
- Default Bridge address: `127.0.0.1`
- Default send port: `9010`
- Default acknowledgement port: `9002`

The normative Plugin-to-Bridge protocol is [Joy-Con Bridge API Contract](joycon-bridge-contract.md). This document defines the Plugin Package behavior that uses it.

## Configuration

The Plugin Package card exposes Bridge Address and Port fields. It derives the OSC sender endpoint from these values and remains inactive when either value is invalid. The Bridge listen port must match the plugin Port. The acknowledgement receiver remains on the separate acknowledgement port.

The address is passed directly to Resonite's OSC sender. The plugin does not resolve or rewrite host names; use a numeric IP address unless host-name support has been verified independently.

## Target Routing

Target is the configured controller address name. Defaults are:

- `left`: left controller, numeric ID `1`
- `right`: right controller, numeric ID `2`

Unknown Target values are not sent. Row number is not a device address.

## Sensation Output

For each valid row, the Plugin sends normalized Force, Vibration, and Pain through the contract's sensation messages. Temperature is reserved and is not sent in Bridge API `0.1.0`.

## Registration And Acknowledgement

When the host becomes locally active, the plugin registers its acknowledgement port with the bridge once per second until a heartbeat is received. It sends sensation values only while heartbeat is current. If heartbeat expires, it stops output, reports disconnected, and resumes registration attempts.

When the host becomes inactive, the plugin stops its receiver and releases the acknowledgement port.

## Bridge Requirements

The bridge uses a fixed-rate output loop. Incoming OSC updates replace the latest desired state; the loop writes only the newest state and does not queue stale HID writes. A final zero update stops output promptly.

Bridge configuration stores listen port, controller address names, numeric IDs, and optional Bluetooth-address bindings. The default `auto` binding selects controllers by side. Command-line values override the file. Explicit save writes effective command-line values back to the file.
