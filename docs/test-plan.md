# Test Plan

Document version: `0.2.0`

## Contract 2 Row List Regression

- Verify all four vector lanes on every row reach the expected plugin Target.
- Change list elements without changing the reference and verify output follows.
- Clear the Rows reference, remove its variable, or supply a list whose count is
  zero or differs from Targets; verify the plugin clears device output and becomes inactive.
- Reconnect valid equal-length lists and verify output recovers.
- Verify one-, four-, five-, and eight-row fixtures route every row by index.
- Repeat in-place edits, missing/null references and unequal counts for Targets.
- Clear ActivePluginId or select a different ID and verify output stops.
- Exercise ConnectionState 0, 1, 2 and an unrecognized value; unknown values
  must show neither connected nor disconnected. Stopping or replacing the
  active plugin must clear stale connection state.
- Verify row-local user overrides and owner-only writes from two clients.
- Repeat independent 80 ms hold, invalid Source/Target clearing and ejection tests.
- Confirm contract-1 plugins cannot emit from a contract-2 Host.

## Static Tests

- Version references agree with `versions.json`.
- Required specifications exist.
- Slot paths and component aliases are unique.
- Every field, component, and local slot reference resolves.
- No executable artifact contains a session-specific runtime ID.
- Each plugin declares contract `2` and the required Manifest fields.
- The local publication policy check passes.

## Host Tests

- World Root state is inactive and shows `Install`.
- Install moves below the clicking user's active avatar and changes actions to `Uninstall`.
- Uninstall returns to World Root and restores inactive state.
- Closing while uninstalled destroys the host; closing while installed only hides the panel.
- Hidden UI does not accept laser input.
- Panel reset is disabled while uninstalled and places an installed panel in front of local Head.

## Sampler Tests

- Node mode resolves the selected BodyNode.
- Slot mode follows the assigned Source slot.
- Null Source disables output and clears transform offset.
- Visual toggle drives `ShowDebugVisual` only for its row.
- Edit preserves source tracking, stores avatar-relative offset, and reset clears it.

## Plugin Tests

- Direct card drop installs without Inspector use.
- Inspector reference assignment reaches the same Package Manager path.
- Replacing a plugin ejects the old card without destroying it.
- Unknown connection state is visually distinct from confirmed disconnection.
- Joy-Con output stops on heartbeat timeout.
- Haptira Target values are strictly `00` through `15`.
- Demo Target `left` and `right` independently vibrate only their matching package-owned device and return it to its base position at zero intensity.

## Joy-Con Bridge Integration Tests

- The Joy-Con Plugin messages and connection behavior match Bridge API `0.1.0`.
- Hardware verification covers Force, Vibration, Pain, acknowledgement, disconnect, and reconnect against a declared compatible Bridge release.
- Bridge implementation and packaging tests run in [byohaptics/joycon-bridge](https://github.com/byohaptics/joycon-bridge).

## Multi-user Matrix

Test both world-owner and visitor installation. For each user, confirm only the local installed host samples, opens a receiver, reports connection state, and emits device output.

- With two clients present, sample `Present`, `Connected`, and `Disconnected` repeatedly and confirm the owner Host publishes one stable, mutually exclusive state.
- Confirm the non-owner client does not change Output Bus values, plugin diagnostics, sender/listener state, or connection Dynamic Variables.
- Eject the plugin and confirm its transport stops and its World Root card becomes visible and grabbable.

## Active and automatic Source selection

- All four rows default Active=true, BodyNode mode and NONE; previous/next arrows are visible.
- Drop a Slot reference on the BodyNode Source field: the reference receiver
  targets that row only, the field changes to its Slot display with no arrows and tracking
  follows that Slot. Drop another Slot while in manual mode to replace it.
- Disable Active in either mode: keep Source text, reference, BodyNode selection
  and offsets; sampler.Enabled=false and the output bus/holds become zero.
- Edit Source while inactive; re-enable and follow the new Source without
  restoring a held pulse. Other rows remain independent.
- Clear from either mode, including while inactive: manual reference=null,
  BodyNode=NONE, BodyNode picker visible, offset reset and output stopped;
  Active remains unchanged. BodyNode selection and previous/next arrows are visible again.
- Repeat with remote users to confirm the existing local-owner sampling gate.
