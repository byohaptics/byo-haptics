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

Set every Manifest field in the contract's
[Required Package Structure](output-plugin-contract.md#required-package-structure).
Use [Connection State](output-plugin-contract.md#connection-state) to choose
`CanReportConnection` for the device.

Update `Package/Visual/Label` to show the same display name, version, contract, and transport to users.

The template License values are examples, not BYO Haptics policy. Set `CreditString`, export permission, and credit requirements for the plugin author's own work and included assets.

## 4. Implement Output

Replace the template's diagnostic field write with the device output while
retaining its Bus readers and owner-gated update path. Keep the test that
`PluginRoot` is under `UserRootSlot(LocalUser)` in its `ActiveState` gate.

The implementation must follow these contract sections:

- [Host Variables](output-plugin-contract.md#host-variables): list types, relays, iteration, validity and routing.
- [Plugin Activation](output-plugin-contract.md#plugin-activation): ownership, final zero output, listener release and OSC shutdown.
- [Connection State](output-plugin-contract.md#connection-state): reporting capability and status publication.
- [Mapping Boundary](output-plugin-contract.md#mapping-boundary): device-specific sensation handling.

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

Custom wrappers must follow the [SlotSpec build procedure](slotspec.md#build-order),
including pending IDs and finalization, as shown above.

## 7. Live Acceptance

Before distribution, run the [row-list regression](test-plan.md#contract-2-row-list-regression),
[plugin tests](test-plan.md#plugin-tests) and [multi-user matrix](test-plan.md#multi-user-matrix)
with the custom plugin. These include card presentation, inventory round trips,
output stopping and connection reporting.

Built-in examples cover three useful designs: direct scene output in the Demo plugin, acknowledged OSC in the Joy-Con plugin, and unacknowledged OSC with bounded zero resend in the Haptira plugin.
