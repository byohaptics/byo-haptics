# SlotSpec Schema

Document version: `0.2.0`

SlotSpec is the declarative source for slots, components, initial member values, references, and ProtoGraph inputs. It never stores runtime IDs.

## Root Object

```json
{
  "schema": "resonite-slotspec/v1",
  "toolName": "BYOHaptics",
  "toolParent": { "kind": "worldRoot" },
  "slots": [],
  "fieldReferences": [],
  "componentReferences": [],
  "localSlotReferences": [],
  "slotFieldReferences": [],
  "sceneSlotReferences": [],
  "sceneComponentReferences": [],
  "fluxInputs": {}
}
```

`toolParent.kind` is `worldRoot` or `avatarRoot`. Avatar-root builds also provide `avatarRoot.name` for build-time lookup.

## Slot

```json
{
  "path": "UI/Panel",
  "position": { "x": 0, "y": 0, "z": 0 },
  "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
  "scale": { "x": 1, "y": 1, "z": 1 },
  "orderOffset": 0,
  "isActive": true,
  "components": []
}
```

`path` is relative to the tool root. Empty path means the tool root. Missing parent paths are created automatically.

## Component And Member

```json
{
  "alias": "config.globalGain",
  "type": "[FrooxEngine]FrooxEngine.ValueField<float>",
  "members": {
    "Value": { "type": "float", "value": 1.0 }
  }
}
```

Aliases are unique across the complete specification. Supported value types are:

- `bool`, `int`, `float`, `string`, `uri`
- `float2`, `float3`, `float4`, `floatQ`
- `colorX` with optional color profile
- `enum` with `enumType`
- `texture2DRawData`
- `stringList`, `float3List`, `float4List`
- `syncObject`, whose nested members use the same syntax
- `componentRef`, containing one component alias and target type
- `componentRefList`, containing component aliases and target type

Asset URLs may be stable content-addressed resources. Scene component and member IDs are prohibited.

## References

`fieldReferences` connects a component member to one field path with `fieldRef`, or to a list of field paths with `fieldRefList`. Each entry must use exactly one form.

```json
{
  "component": "ui.gain.copy",
  "member": "Source",
  "fieldRef": "config.globalGain.Value",
  "targetType": "[FrooxEngine]FrooxEngine.IField<float>"
}
```

For example, `ValueMultiDriver<T>.Drives` uses the list form:

```json
{
  "component": "ui.visibility.driver",
  "member": "Drives",
  "fieldRefList": [
    "ui.panel.Enabled",
    "ui.label.Enabled"
  ],
  "targetType": "[FrooxEngine]FrooxEngine.IField<bool>"
}
```

Nested paths such as `runtime.sender.HandlingUser.User` are valid.

For the [OSC transport rules](output-plugin-contract.md#plugin-activation),
bind `SenderEnabled` to `runtime.sender.Enabled`, initially false, with a
nonpersistent `ValueUserOverride<bool>` defaulting to false. Engine status
ownership was verified in `OSC_Sender.OnCommonUpdate` and `OSC_Handler.OnChanges`
in build `2026.9.18.82`.

List elements use a zero-based numeric path segment, for example
`output.bus.rows.Values.0`. A reference to `output.bus.rows.Values` addresses
the list itself. `float4List` initializes a `SyncFieldList<float4>` using an
array of `{ "x": 0, "y": 0, "z": 0, "w": 0 }` objects.

The Host bus uses `float4List` for Rows and `stringList` for Targets. Their
runtime types, publication, collection readers and validity rules are defined
in [Host Variables](output-plugin-contract.md#host-variables). SlotSpec numeric
element paths are not limited to indices 0 through 3.

`componentReferences` resolves a component alias. `localSlotReferences` resolves a slot path in this specification. `slotFieldReferences` resolves a slot member such as `IsActive`. Scene references are permitted only for explicitly named build context and must fail when ambiguous.

Built-in provider IDs are not supported. Required fonts, sprites, textures, and materials are packaged and referenced by component alias.

The reflected UIX mask type is `[FrooxEngine]FrooxEngine.UIX.Mask`. Its `ShowMaskGraphic` boolean keeps the same-slot graphic visible while clipping descendant UIX graphics to that graphic's shape.

## ProtoGraph Inputs

Each input uses exactly one source:

```json
{
  "ToolRoot": { "slotPath": "" },
  "Installed": { "fieldRef": "config.installed.Value" },
  "InstallButton": { "componentRef": "ui.install.button" },
  "SamplersActive": {
    "slotField": { "slotPath": "Samplers", "slotMember": "IsActive" }
  }
}
```

## Build Order

1. Validate schema and component types; compile graphs before wrapper deployment.
2. Resolve the declared parent and create an inactive staging root and slots.
3. Create components without deferred references.
4. Create components containing component references.
5. Resolve fields, components, local slots, and slot fields.
6. Resolve explicitly allowed scene references.
7. Write generated IDs for slots, components, members, and ProtoGraph inputs.
8. Deploy and wire graphs, then finalize the staged root. Only then replace the
   previous item and activate the new root.

Any missing alias, path, component type, ambiguous scene match, or unsupported member type stops the build.

Recreation searches only direct children of the declared parent, matching both
name and tag. Duplicate names or a mismatched tag stop the build. Cached IDs and
world-wide name searches never authorize deletion. Finalization rechecks the
parent, name and tag of both roots before replacing anything. If building or
graph deployment fails, the old item and its generated IDs remain intact; an
inactive staging item may remain for inspection. Installed items outside the
declared parent are never replaced automatically.

If final activation fails, restore the previous root's active state. If removal
of the old root fails after activation succeeds, leave the new item usable and
the old item disabled, report the error, and retain pending IDs for inspection.
Connection loss can prevent rollback; inspect both roots before retrying.

The builder writes `<output>.pending.json`. Consumers deploy using that file,
then call `finalize-slotspec-build.fsx` with `RESONITE_SLOTSPEC_OUTPUT` set to the
normal output path. Successful finalization publishes the normal generated IDs.
This also applies to SlotSpecs without graphs: explicitly finalize after building.

Until finalization, the normal IDs file continues to identify the previous
item. Deployment consumers reject that file while a pending build exists, so
an outdated wrapper cannot update the previous item's graphs. Custom and
independently maintained wrappers must use the same pending/deploy/finalize
sequence.

## Generated IDs

Generated IDs are valid only for the current Resonite session and the object produced by that build. Deployment requires the generated file and never falls back to constants. Generated files live under `build/` and are not committed.

## Source UI references

The Host Source UI uses these native types:

- `[FrooxEngine]FrooxEngine.ReferenceEqualityDriver<[FrooxEngine]FrooxEngine.Slot>`:
  `TargetReference` relays the row's `SyncRef<Slot>` field; `Reference` defaults
  null. With `Invert=false`, `Target` drives the existing UseBodyNode bool true
  exactly when the manual Source is null. This works without an installed Host
  and requires no per-client ProtoFlux configuration writer.
- `[FrooxEngine]FrooxEngine.UIX.ReferenceReceiver`: `TargetReference` points to
  the row's Source `ISyncRef`; `Undoable=true` preserves native drop undo. It is
  attached to the BodyNode value button's Slot so that a Slot reference proxy
  can be dropped before manual mode is visible. The existing RefEditor handles
  replacement drops in manual mode. Native receiver validation preserves the
  target's Slot type constraint.

No SlotSpec schema or builder extension is needed. Manual/BodyNode visibility
remains driven by UseBodyNode, independently of the row's Active checkbox.
