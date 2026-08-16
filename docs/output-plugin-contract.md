# Output Plugin Contract

Document version: `0.1.9`

## Contract Identity

- Contract name: `BYOHaptics.Output.v1`
- Contract integer: `1`
- Socket cardinality: exactly zero or one direct-child plugin
- Required plugin tag: `byo-haptics.output-plugin`

## Required Package Structure

```text
<PluginId>                         Tag = byo-haptics.output-plugin
  Manifest
    PluginId                       string
    DisplayName                    string
    ContractVersion                int = 1
    CanReportConnection            bool
    Transport                      string
    PluginVersion                  string
    Author                         string
  Config                           plugin-owned
  Runtime                          plugin-owned
  Diagnostics
    Active                         bool
    Connected                      bool
  Flux
  Package
    DropReference                  ReferenceField<Slot> -> plugin root
    Visual
    State/Installed                bool
```

`PluginId` uses a reverse-domain namespace. Built-in IDs are:

- `io.github.byohaptics.output.joycon.osc`
- `io.github.byohaptics.output.haptira.osc`
- `io.github.byohaptics.output.demo`

## Host Variables

All variables use the `BYOHaptics.Output.v1/` prefix.

| Name | Type | Meaning |
| --- | --- | --- |
| `Active` | bool | Local output is allowed |
| `SelectedPluginId` | string | Selected plugin root name |
| `ContractVersion` | int | Must equal `1` |
| `Row0Force` through `Row3Force` | float | Normalized Force |
| `Row0Vibration` through `Row3Vibration` | float | Normalized Vibration |
| `Row0Pain` through `Row3Pain` | float | Normalized Pain |
| `Row0Temperature` through `Row3Temperature` | float | Normalized Temperature |
| `Target0` through `Target3` | string | Transport-neutral destination |
| `Connected` | bool | External receiver is available |
| `ConnectionStatusAvailable` | bool | Plugin can determine receiver state |

The host clamps each sensation after applying row and global gain. If Source is null, the row is disabled, or Target is empty, all four output values for that row are zero.

## Plugin Activation

A plugin emits output only when all conditions are true:

1. `Active` is true.
2. `SelectedPluginId` equals its `PluginId`.
3. `ContractVersion` equals `1`.
4. Its device-specific configuration is valid.

When inactive, the plugin sends or stores a final zero state and releases listeners it owns.

## Connection State

- Unknown: `ConnectionStatusAvailable=false`.
- Connected: both values are true.
- Disconnected: status is available and `Connected=false`.

The UI displays unknown as a gray link symbol, connected as green, and disconnected as a gray link with a red cross.

## Mapping Boundary

The host does not combine or reinterpret sensation types for a device. Each plugin receives all four values and decides how to drive its target device. Unsupported sensations are ignored by that plugin.

## Package Lifecycle

Dropping a tagged plugin card on the Package field sets one `PackageReference`. The Package Manager validates the candidate, ejects an existing plugin without destroying it, reparents the candidate under the socket, and resets its local transform. Eject reverses the operation and restores the card in World Root.

Physical card drop and Inspector reference assignment converge on the same `PackageReference`; installation logic is not duplicated.
