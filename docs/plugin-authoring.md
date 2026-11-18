# Output Plugin Authoring

Document version: `0.2.0`

This guide creates a BYO Haptics Output Plugin Package from the repository template. Read [the runtime contract](output-plugin-contract.md), [the package lifecycle](output-plugin-package.md), and [the SlotSpec schema](slotspec.md) when changing the template structure.

## 1. Copy The Template

Copy both files from `templates/output-plugin/` into project-owned paths:

- `minimal-output-plugin.resoslots.json`: package structure and generated references
- `MinimalOutputPlugin.pg`: runtime behavior

The template is deliberately small. Target `default` routes the maximum Force value from every published row to `Diagnostics/LastForce`. Replace that field write with the actual scene, OSC, WebSocket, or device output.

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
| `ContractVersion` | `2` |
| `CanReportConnection` | `true` only when the plugin has positive receiver status |
| `Transport` | Short description such as `osc`, `websocket`, or `scene` |
| `PluginVersion` | Semantic version prefixed with `v` |
| `Author` | Plugin author's name |

Update `Package/Visual/Label` to show the same display name, version, contract, and transport to users.

The template License values are examples, not BYO Haptics policy. Set `CreditString`, export permission, and credit requirements for the plugin author's own work and included assets.

## 4. Implement Output

The Host publishes normalized values under `BYOHaptics.Output.v2/`. A plugin must:

1. Declare Manifest `ContractVersion=2` and require v2 `ActivePluginId` to equal its own non-empty Plugin ID. Empty/missing means stopped; there is no separate Active or ContractVersion DV.
2. Read `Rows` as a `SyncFieldList<float4>` reference, pass it through `ContinuouslyChangingObjectRelay<SyncFieldList<float4>>`, require a non-empty list, and interpret X/Y/Z/W as Force/Vibration/Pain/Temperature. The relay refreshes reads when only elements change. Never write to or resize this Host-owned list.
3. Read `Targets` as a `SyncFieldList<string>` reference through its own continuously-changing relay. Require `Targets.Count == Rows.Count`, then iterate every row by index with `ReadOnlyForEachWithIndexValue`. An invalid or mismatched list stops output; an empty string disables that row.
4. Map supported Force, Vibration, Pain, and Temperature values to its device.
5. Write or send zero when inactive, deselected, invalid, or stopped.
6. Keep device behavior and transport details inside the plugin.

Unsupported sensations and unknown Targets may be ignored. Handle every published row even when several rows share one destination; use the device-appropriate combination rule, normally maximum intensity rather than addition. Do not add a separate row-count variable: the list count is authoritative.

Publish `ConnectionState` from the existing owner-gated update path: `0=Unknown`,
`1=Connected`, `2=Disconnected`. Connected requires positive receiver evidence.
If state cannot be known, or the plugin is inactive, publish Unknown. Use named
integer constants; do not introduce independent status booleans on the bus.

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
if ($LASTEXITCODE -ne 0) { throw 'SlotSpec build failed; do not use previous pending IDs.' }

$env:OUTPUT_PLUGIN_MODULE = 'templates/output-plugin/MinimalOutputPlugin'
$env:OUTPUT_PLUGIN_IDS = 'build/minimal-output-plugin.generated-ids.json.pending.json'
$env:OUTPUT_PLUGIN_DEPLOY = '1'
./scripts/run-fluxsdk-script.ps1 -ScriptPath scripts/deploy-output-plugin.fsx
if ($LASTEXITCODE -ne 0) { throw 'Plugin deployment failed; previous item was retained.' }
./scripts/run-fluxsdk-script.ps1 -ScriptPath scripts/finalize-slotspec-build.fsx
```

Generated IDs are session-local build artifacts. Never commit them or copy them into SlotSpec or ProtoGraph source.

Custom and independently maintained plugin wrappers must consume the new
`.pending.json` file and run the finalizer only after successful graph deployment.
The normal IDs file continues to identify the previous item until finalization.
Consumers reject that normal file while a pending build exists, so an outdated
wrapper fails safely instead of updating the previous item's graphs.

## 7. Live Acceptance

Before distribution:

1. Confirm the World Root card shows name, version, contract, and transport.
2. Drop the card into BYO Haptics and confirm its visual and Grabbable disable.
3. Exercise the current Host's Rows 0 through 3, then test a fixture with more than four paired Rows and Targets.
4. Confirm inactive, deselected, empty Target, ejection, and receiver loss produce final zero output.
5. Confirm connection status is connected, disconnected, or unknown according to actual evidence.
6. Eject, save to inventory, spawn in another world, and reinstall.
7. Test from each participating client when multi-user behavior matters.

An installed plugin must compare `PluginRoot` with `UserRootSlot(LocalUser)` and run mutable runtime or transport impulses only on the containing user's client. Include that ownership condition in `ActiveState`; do not let a remote client repeatedly write false or zero into shared connection and output fields. The tracked minimal template demonstrates this gate while still allowing an uninstalled World Root card to converge to its stopped, visible state.

Built-in examples cover three useful designs: direct scene output in the Demo plugin, acknowledged OSC in the Joy-Con plugin, and unacknowledged OSC with bounded zero resend in the Haptira plugin.
