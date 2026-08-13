# Sampler Specification

Document version: `0.1.3`

## Row State

Each of four rows owns:

- Target string
- Node-mode boolean
- BodyNode selection
- manual Source slot reference
- resolved Source slot
- row gain
- Visual boolean
- sampler edit boolean
- avatar-relative position and rotation offset
- one `VirtualHapticPointSampler`

## Source Resolution

```text
ResolvedSource = NodeMode ? ActiveAvatar.BodyNodeSlot : ManualSource
RowValid = ResolvedSource != null && Target != "" && RowEnabled
```

Node mode is the default. Manual Source is never inferred from Target.

Clearing Source sets manual Source null, disables output, resets sampler local transform, and clears stored offsets. Selecting a valid BodyNode after clearing starts from zero offset.

## Sampling

For each sensation `S`:

```text
OutputS = RowValid ? clamp01(Sampler.S * RowGain * GlobalGain) : 0
```

The four sensations remain separate on the Output Bus. Debug visual state is observational and is never used as a signal value.

## Local-user Isolation

The `Samplers` slot is active only for the local client whose User Root contains the tool. Remote clients keep it inactive. This controls the sampler subtree as one unit instead of individually driving every sampler component.

## Edit Offset

Normal and edit modes both retain Source tracking. The sampler transform is:

```text
SamplerGlobal = SourceGlobal * AvatarRelativeOffset
```

When edit starts, the sampler becomes grabbable without removing Source tracking. On release, calculate the offset in avatar-local coordinates from the new sampler pose. When edit ends, disable grabbing but retain the offset. `Reset All Positions` sets every offset to identity.

A row with null ResolvedSource is omitted from the Sampler Edit submenu.
