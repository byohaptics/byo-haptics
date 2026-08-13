# SlotSpec Schema

Document version: `0.1.1`

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
  "alias": "config.version",
  "type": "[FrooxEngine]FrooxEngine.ValueField<string>",
  "members": {
    "Value": { "type": "string", "value": "v0.1.0" }
  }
}
```

Aliases are unique across the complete specification. Supported value types are:

- `bool`, `int`, `float`, `string`, `uri`
- `float2`, `float3`, `float4`, `floatQ`
- `colorX` with optional color profile
- `enum` with `enumType`
- `texture2DRawData`
- `stringList`, `float3List`
- `syncObject`, whose nested members use the same syntax
- `componentRef`, containing one component alias and target type
- `componentRefList`, containing component aliases and target type

Asset URLs may be stable content-addressed resources. Scene component and member IDs are prohibited.

## References

`fieldReferences` connects a component member to another component's field path:

```json
{
  "component": "ui.version.copy",
  "member": "Source",
  "fieldRef": "config.version.Value",
  "targetType": "[FrooxEngine]FrooxEngine.IField<string>"
}
```

Nested paths such as `runtime.sender.HandlingUser.User` are valid.

`componentReferences` resolves a component alias. `localSlotReferences` resolves a slot path in this specification. `slotFieldReferences` resolves a slot member such as `IsActive`. Scene references are permitted only for explicitly named build context and must fail when ambiguous.

Built-in provider IDs are not supported. Required fonts, sprites, textures, and materials are packaged and referenced by component alias.

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

1. Validate schema and component types.
2. Resolve or create the tool root and slots.
3. Create components without deferred references.
4. Create components containing component references.
5. Resolve fields, components, local slots, and slot fields.
6. Resolve explicitly allowed scene references.
7. Write generated IDs for slots, components, members, and ProtoGraph inputs.

Any missing alias, path, component type, ambiguous scene match, or unsupported member type stops the build.

## Generated IDs

Generated IDs are valid only for the current Resonite session and the object produced by that build. Deployment requires the generated file and never falls back to constants. Generated files live under `build/` and are not committed.
