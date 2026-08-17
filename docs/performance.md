# Host Lightweight Plan

Document version: `0.1.9`

## Measurements

| Stage | Fixed SlotSpec slots | Live total slots | Change from baseline |
| --- | ---: | ---: | ---: |
| Baseline reported in scene | 294 | 2088 | — |
| Remove read-only diagnostic mirrors | 253 | 1916 | -172 |
| Merge Source Binding into Sampler | 245 | 1588 | -500 |
| Repair Source clearing and remove its intermediate activation state | 245 | 1568 | -520 |
| Vectorize Output Bus hold state and timing | 245 | 1403 | -685 |
| Consolidate internal state and duplicate UI wrappers; delete unused state | 174 | 1331 | -757 |

The completed reductions remove 36.3% of live slots without changing four-row sampling, Node and Slot source modes, offsets, minimum pulse retention, plugin replacement, or transport behavior. Automatic Target suggestion was removed; Target remains an explicit routing setting.

## Remaining Priority

Visually inspect the compacted panel and regression-test Node and Slot sources, clear actions, offsets, plugin status, and minimum pulse retention in VR.

## Output Bus Vectorization Plan

Replace the 16 scalar hold fields with four `float4` fields, one per row. Within each vector, X, Y, Z, and W represent Force, Vibration, Pain, and Temperature. Pack each row's sampled values before gain and clamp processing, then unpack only at the scalar Output Bus boundary.

Use the existing per-frame `LocalUpdate` with `DeltaTime` to decrement each vector lane and remove the separate 10 ms `SecondsTimer`. A positive sensation resets only its own lane. An inactive row clears all four lanes immediately.

The following behavior must remain unchanged:

- each row and sensation has an independent retained value and expiry;
- a Force pulse cannot extend Vibration, Pain, or Temperature;
- a pulse on one row cannot extend another row;
- a new positive sample replaces only that sensation's retained strength and restarts only its duration;
- an invalid Host, row, Source, or Target clears all outputs for that row immediately;
- `MinimumPulseDuration = 0` disables retention;
- the public Output Bus remains 16 independent scalar values.

The vectorized implementation reduced fixed Host components from 662 to 650, Output Bus generated node components from 576 to 411, and Output Bus deployment responses from 1429 to 1048. Both the Local development deployment and the focused `byohaptics World` deployment contained 1403 total slots and produced zero module-discovery errors or input-reference corrections. Static tests and compilation pass. Live readback in `byohaptics World` confirmed all six sheets active, the 80 ms default, and zeroed independent row hold vectors. Perceptual confirmation after vectorization remains pending.

## Internal And UI Consolidation

Keep the effective public configuration children `GlobalGain` and `MinimumPulseDuration`. Remove the unreferenced `Enabled`, `SendInterval`, `DefaultRadius`, and duplicated `Version` fields; the panel title remains the visible product-version source. Move row bookkeeping, installed state, and panel visibility components to `Config/State`. Keep all Output Plugin contract component aliases and Dynamic Variable names, but place their components directly on `Outputs/PluginSocket/Bus`. Place the three plugin-state indicator fields on `Diagnostics/Plugin`.

Remove only UI wrapper Slots whose sole child repeats the same full-stretch rectangle and layout size. The Target, Visual, and Node-mode controls retain their dimensions and move their existing components to the sizing Slot. Column-header text and title text move to their existing sizing Slots. Footer button controls move to their identical-width area Slots. Sampler Edit entries are enabled directly instead of copying four constant `true` fields into twelve components. These changes preserve behavior while reducing fixed structure from 245 to 174 Slots and from 650 to 597 components. The deployed Host fell from 1403 to 1331 live slots, a further reduction of 72.
