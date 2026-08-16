# Test Plan

Document version: `0.1.9`

## Static Tests

- Version references agree with `versions.json`.
- Required specifications exist.
- Slot paths and component aliases are unique.
- Every field, component, and local slot reference resolves.
- No executable artifact contains a session-specific runtime ID.
- Each plugin declares contract `1` and the required Manifest fields.
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

## Joy-Con Bridge Tests

- Rust unit tests pass for OSC parsing, routing, heartbeat timeout, scheduling, sensation mapping, HID encoding, and configuration precedence.
- Public defaults and examples contain no real Bluetooth address.
- `--dry-run` accepts the documented messages and stops state after heartbeat timeout.
- The GUI and CLI compile from the same Bridge source and configuration contract.
- Hardware verification covers Force, Vibration, Pain, acknowledgement, disconnect, and reconnect.

## Multi-user Matrix

Test both world-owner and visitor installation. For each user, confirm only the local installed host samples, opens a receiver, reports connection state, and emits device output.
