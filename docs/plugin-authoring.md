# Output Plugin Authoring

Document version: `0.1.9`

This guide creates a BYO Haptics Output Plugin Package from the repository template. Read [the runtime contract](output-plugin-contract.md), [the package lifecycle](output-plugin-package.md), and [the SlotSpec schema](slotspec.md) when changing the template structure.

## 1. Copy The Template

Copy both files from `templates/output-plugin/` into project-owned paths:

- `minimal-output-plugin.resoslots.json`: package structure and generated references
- `MinimalOutputPlugin.pg`: runtime behavior

The template is deliberately small. Target `default` routes the maximum Force value from Rows 0 through 3 to `Diagnostics/LastForce`. Replace that field write with the actual scene, OSC, WebSocket, or device output.

## 2. Choose Stable Identity

Use a globally unique reverse-domain Plugin ID, for example:

```text
io.github.example.output.minimal
com.example.haptics.output.device
```

Use the same value in exactly these places:

1. SlotSpec `toolName`
2. Manifest `PluginId`
3. ProtoGraph `ThisPluginId`

Do not include a version in the ID. Do not change it after publication. Display name, version, author, transport, and card text may change independently. A third-party plugin manages its version in its own project and does not add itself to this repository's `versions.json`.

## 3. Set Manifest And License

Set every Manifest value:

| Field | Rule |
| --- | --- |
| `PluginId` | Stable reverse-domain ID |
| `DisplayName` | User-facing name |
| `ContractVersion` | `1` |
| `CanReportConnection` | `true` only when the plugin has positive receiver status |
| `Transport` | Short description such as `osc`, `websocket`, or `scene` |
| `PluginVersion` | Semantic version prefixed with `v` |
| `Author` | Plugin author's name |

Update `Package/Visual/Label` to show the same display name, version, contract, and transport to users.

The template License values are examples, not BYO Haptics policy. Set `CreditString`, export permission, and credit requirements for the plugin author's own work and included assets.

## 4. Implement Output

The Host publishes normalized values under `BYOHaptics.Output.v1/`. A plugin must:

1. Require `Active=true`, `ContractVersion=1`, and its own selected Plugin ID.
2. Interpret each row's Target using plugin-defined rules.
3. Map supported Force, Vibration, Pain, and Temperature values to its device.
4. Write or send zero when inactive, deselected, invalid, or stopped.
5. Keep device behavior and transport details inside the plugin.

Unsupported sensations may be ignored. Handle all four rows even when they share one destination; use the device-appropriate combination rule, normally maximum intensity rather than addition.

`Connected=true` means an external receiver was positively confirmed. If that cannot be known, keep `ConnectionStatusAvailable=false`; do not report an assumed connection.

## 5. Validate And Compile

From the repository root:

```powershell
node scripts/validate-slotspec.mjs templates/output-plugin/minimal-output-plugin.resoslots.json
$env:PROTOGRAPH_MODULE = 'templates/output-plugin/MinimalOutputPlugin'
./scripts/run-fluxsdk-script.ps1 -ScriptPath scripts/compile-protograph-module.fsx
```

`npm test` validates the tracked template, and `npm run compile:plugins` compiles it with the built-in plugin graphs.

## 6. Deploy A Custom Plugin

With ResoniteLink running, set the current session URL and use the generic builder and deployment consumer:

```powershell
$env:RESONITELINK_URL = 'ws://localhost:<port>'
$env:RESONITE_SLOTSPEC_PATH = 'templates/output-plugin/minimal-output-plugin.resoslots.json'
$env:RESONITE_SLOTSPEC_OUTPUT = 'build/minimal-output-plugin.generated-ids.json'
$env:RESONITE_SLOTSPEC_RECREATE = '1'
./scripts/run-fluxsdk-script.ps1 -ScriptPath scripts/build-from-resonite-slotspec.fsx

$env:OUTPUT_PLUGIN_MODULE = 'templates/output-plugin/MinimalOutputPlugin'
$env:OUTPUT_PLUGIN_IDS = 'build/minimal-output-plugin.generated-ids.json'
$env:OUTPUT_PLUGIN_DEPLOY = '1'
./scripts/run-fluxsdk-script.ps1 -ScriptPath scripts/deploy-output-plugin.fsx
```

Generated IDs are session-local build artifacts. Never commit them or copy them into SlotSpec or ProtoGraph source.

## 7. Live Acceptance

Before distribution:

1. Confirm the World Root card shows name, version, contract, and transport.
2. Drop the card into BYO Haptics and confirm its visual and Grabbable disable.
3. Exercise Rows 0 through 3 and every supported sensation and Target.
4. Confirm inactive, deselected, empty Target, ejection, and receiver loss produce final zero output.
5. Confirm connection status is connected, disconnected, or unknown according to actual evidence.
6. Eject, save to inventory, spawn in another world, and reinstall.
7. Test from each participating client when multi-user behavior matters.

Built-in examples cover three useful designs: direct scene output in the Demo plugin, acknowledged OSC in the Joy-Con plugin, and unacknowledged OSC with bounded zero resend in the Haptira plugin.
