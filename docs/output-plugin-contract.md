# Output Plugin Contract

Document version: `0.2.0`

## Contract Identity

- Contract name: `BYOHaptics.Output.v2`
- Contract integer: `2`
- Socket cardinality: exactly zero or one direct-child plugin
- Required plugin tag: `byo-haptics.output-plugin`

## Required Package Structure

```text
<PluginId>                         Tag = byo-haptics.output-plugin
  Manifest
    PluginId                       string
    DisplayName                    string
    ContractVersion                int = 2
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

The independently maintained BYO Haptic Actuator plugin uses
`io.github.byohaptics.output.bha.osc`.

## Host Variables

All variables use the `BYOHaptics.Output.v2/` prefix.

| Name | Type | Meaning |
| --- | --- | --- |
| `ActivePluginId` | string | Selected Plugin ID; empty means stopped |
| `Rows` | reference to `SyncFieldList<float4>` | Ordered normalized sensation vectors owned by `ValueList<float4>` |
| `Targets` | reference to `SyncFieldList<string>` | One transport-neutral destination for each row |
| `ConnectionState` | int | `0=Unknown`, `1=Connected`, `2=Disconnected` |

The host clamps each sensation after applying row and global gain. If Source is null, the row is disabled, or Target is empty, all four output values for that row are zero.

`Outputs/PluginSocket/Bus` owns one `ValueList<float4>`. The current Host
initializes four zero vectors, but the contract does not fix a maximum row
count. `Rows.Count` is the row count; no redundant `RowCount` variable is
published. `Rows` publishes its `Values` member using
`DynamicReferenceVariable<SyncFieldList<float4>>`. Indices 0 through 3 correspond
to the current Host's sampler rows. For any Host size, indices `0` through
`Rows.Count - 1` are the ordered rows; X, Y, Z, W mean Force, Vibration, Pain,
Temperature. `float4` fixes the four sensation lanes within one row, not the
number of rows.
The Host writes the existing element fields without resizing or replacing the
list. Plugins only read the list and never mutate it. A missing/null reference
or an empty list makes a plugin inactive and clears its output.
Because element writes do not change the list reference, plugin readers must
pass it through `ContinuouslyChangingObjectRelay<SyncFieldList<float4>>` before
counting or indexing. Otherwise pure collection nodes can retain cached values.

`Targets` likewise publishes `ValueList<string>.Values`. Its count must equal
`Rows.Count`, and index `i` describes `Rows[i]`. The current Host's four Target
fields copy into its four elements. Readers use a continuously-changing object
relay before counting/indexing the target list. Missing/null lists, zero rows,
or unequal counts disable all output; an empty target string disables only its
individual row. A Host that changes cardinality must clear `ActivePluginId`,
resize and initialize both lists, then reactivate output. The count-equality
guard makes any replicated intermediate state fail closed.

Plugins iterate every row rather than naming fixed indices. Multiple rows may
share a Target; the plugin combines them according to its device mapping. The
built-in plugins use maximum intensity rather than addition. Unsupported or
unknown Target values are ignored without invalidating other rows.

Each row and sensation retains its independent minimum-pulse expiry. Packing
the values must not share expiry between lanes or rows. Each row element keeps
its local-user override; installed writes remain owner-client-only.

The public bus has four DVs. No legacy scalar mirrors are published. Namespace v2 and
Manifest `ContractVersion=2` identify compatibility; a redundant ContractVersion
DV is not published. Existing contract-1 readers must update.

The Host writes `ActivePluginId` only when local output is allowed and clears it
otherwise. It resets `ConnectionState` to unknown when the active ID changes or
output stops. The selected Plugin publishes one state from its owner-client
update path; inactive or unreportable states are unknown. Diagnostic booleans
inside the Plugin remain private implementation details, not bus variables.

## Plugin Activation

A plugin emits output only when all conditions are true:

1. Its root is under the current client's `LocalUser` hierarchy.
2. The v2 `ActivePluginId` exists and equals its non-empty `PluginId`.
3. `Rows` and `Targets` are non-null and non-empty, and their counts are equal.
4. Its Manifest declares `ContractVersion=2` and it uses the v2 namespace.
5. Its device-specific configuration is valid.

When inactive, the plugin sends or stores a final zero state and releases listeners it owns.

OSC plugins control transport with `OSC_Sender.Enabled`, locally overridden per
client and false by default. `IsSending` is Resonite-owned status, never a
control input or an override target. On deactivation, clear sensation values
immediately, stop heartbeat generation, and resend only the final zero state
every 50 ms for a bounded 250 ms tail before disabling the sender. Only the
previously active client runs this tail; it must not read new Bus sensations.
Reactivation cancels shutdown through the normal update path.

While installed, only that owner client may update shared Host or Plugin runtime fields, connection variables, timers, listeners, or transport senders. Other clients must not publish an inactive value over the owner's state. At World Root the plugin is inactive; clients may write only the common stopped state and package presentation needed for ejection and inventory use.

## Connection State

- Unknown: `ConnectionState=0` (also the fallback for unrecognized values).
- Connected: `ConnectionState=1`.
- Disconnected: `ConnectionState=2`.

The UI displays unknown as a gray link symbol, connected as green, and disconnected as a gray link with a red cross. The red cross is disabled outside the disconnected state; transparency alone is not used to hide it.

The Host reflects a state change within one update. Indicators are gated by
activation; an inactive Host shows unknown rather than a retained earlier state.
The two indicators compare the same integer with 1 and 2 respectively, so they
cannot both be true. Enum-like names are constants; the DV type is `int`.

## Mapping Boundary

The host does not combine or reinterpret sensation types for a device. Each plugin receives all four values and decides how to drive its target device. Unsupported sensations are ignored by that plugin.

## Package Lifecycle

Dropping a tagged plugin card on the Package field sets one `PackageReference`. The Package Manager validates the candidate, ejects an existing plugin without destroying it, reparents the candidate under the socket, and resets its local transform. Eject reverses the operation and restores the card in World Root.

Physical card drop and Inspector reference assignment converge on the same `PackageReference`; installation logic is not duplicated.

## Independently maintained plugins

BYO Haptic Actuator OSC implements this contract in the separate
`byo-haptic-actuator` repository. Device targets, OSC paths and firmware behavior
are specified and tested there. The host treats its Plugin ID like any other
compatible Contract 2 package and contains no BHA-specific mapping.
