# Sampler Specification

Document version: `0.2.0`

## Row State

Each of four rows owns:

- Target string
- Active boolean (enabled by default)
- derived BodyNode-mode boolean (true when the manual Source is null)
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
UseBodyNode = ManualSource == null
ResolvedSource = UseBodyNode ? ActiveAvatar.BodyNodeSlot : ManualSource
RowValid = ResolvedSource != null && Target != "" && RowEnabled
```

BodyNode selection is the default. A non-null manually dropped Slot reference
takes priority automatically; it is never inferred from Target. Source display
and mode depend on the reference, independently of Active. Clearing or losing
the manual reference returns to the stored BodyNode selection; the clear action
also resets that selection to `NONE`.

Active controls sampler.Enabled and the existing RowEnabled bus gate. Switching
Active off preserves Source resolution, displayed text, selections and offsets,
but disables detection and clears all four sensation hold lanes immediately.
Re-enabling restores sampling from the selected Source without replaying held
output. Source tracking and editing can continue while detection is inactive.

Clearing Source leaves Active unchanged and sets both backing selections to their empty values (manual Source to null and BodyNode to `NONE`), disables output, resets sampler local transform, and clears stored offsets. Selecting a valid Source after clearing starts from zero offset. The clear button uses Resonite's native button set components; ProtoFlux does not synthesize null Slot or `BodyNode.NONE` constants for this action.

## Sampling

For each sensation `S`:

```text
RawOutputS = RowValid ? clamp01(Sampler.S * RowGain * GlobalGain) : 0
OutputS = RawOutputS > 0
    ? RawOutputS
    : HoldRemainingS > 0 ? LastPositiveOutputS : 0
```

The four sensations remain separate on the Output Bus. Debug visual state is observational and is never used as a signal value.

Each row and sensation independently retains its last positive output for at least `MinimumPulseDuration` after the final positive sample. `MinimumPulseDuration` is a Host configuration value in seconds, defaults to `0.08`, and may be set to `0` to disable retention. A new positive sample replaces the retained strength and restarts its duration. Disabling the Host or row, losing Source resolution, or clearing Target bypasses and clears retention immediately.

## Local-user Isolation

Implement the [lifecycle activity gate](lifecycle.md#derived-state) on the
sampler subtree as one unit. An always-active Host-root `ValueUserOverride<bool>`
drives `Samplers.IsActive`; ProtoFlux writes that driven field to create the
local user's override. The controller must not live on its target subtree.

## Edit Offset

Normal and edit modes both retain Source tracking. The sampler transform is:

```text
SamplerGlobal = SourceGlobal * AvatarRelativeOffset
```

When edit starts, the sampler becomes grabbable without removing Source tracking. On release, calculate the offset in avatar-local coordinates from the new sampler pose. When edit ends, disable grabbing but retain the offset. `Reset All Positions` sets every offset to identity.

Row selection and labels are defined in the [context menu specification](host-ui.md#context-menu).
