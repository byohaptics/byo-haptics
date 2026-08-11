# Joy-Con Rumble Bridge

Version `0.1.0`

This Windows bridge receives the BYO Haptics Joy-Con OSC protocol and drives paired Joy-Con controllers over Bluetooth HID.

## Setup

1. Pair the controllers in Windows.
2. List their HID serial addresses:

   ```powershell
   cargo run -- joycon-list
   ```

3. Save the addresses to a local configuration:

   ```powershell
   cargo run -- --device left,auto,left,1 --device right,auto,right,2 --save-config
   ```

`auto` selects the first connected controller of each side. Specify a Bluetooth address only to distinguish multiple controllers of the same side. Local `joycon-rumble.toml` files are ignored by Git.

## Run

```powershell
cargo run --release
```

The GUI can scan devices, run IMU calibration, and start or stop the Bridge:

```powershell
cargo run --release --bin joycon-rumble-gui
```

Use `cargo run -- --dry-run` to inspect received output without opening controllers.

## Defaults

- OSC receiver: `127.0.0.1:9010`
- namespace: `joyconrumble`
- heartbeat timeout: `2000 ms`
- Target names: `left`, `right`
- player IDs: `1`, `2`

The complete wire contract is in [Joy-Con Bridge](../../docs/joycon-bridge.md).

Copyright 2026 byohaptica. All rights reserved.
