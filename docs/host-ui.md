# Host UI Specification

Document version: `0.1.9`

## Panel

- Canvas size: `800 x 700` UI units.
- UI slot scale: `0.00055` on all axes.
- Canvas ignores touches from behind.
- Backing is opaque and hides controls from the rear.
- Panel, input, and button backgrounds use packaged rounded assets.
- Panel is grabbable; hidden panel disables its collider and interaction.
- Title: `BYO Haptics v0.2.0`.
- Close is a compact red circular button in the title area.

## Main Order

1. Title
2. Output Plugin row
3. Column headers
4. Four sampler rows
5. `Panel Position Reset` and `Install` or `Uninstall`

## Output Plugin Row

Left to right:

1. Plugin state symbol, width `60`
2. Package reference field, preferred width `500`
3. Eject icon button, width `60`
4. Connection state symbol, width `60`

The Package field supports physical card drop and Inspector reference assignment. It displays the installed plugin name. The plugin state symbol is gray when empty and green when present. The connection symbol follows the three states in the plugin contract.

## Sampler Columns

Left to right:

1. `Target`
2. `Node`
3. `Source`
4. Clear action
5. `Visual`

The `Node` checkbox selects BodyNode mode when checked and manual Slot mode when unchecked. Clear is immediately to the right of Source. Visual is last so it cannot be read as a Target setting.

Each row is the same height and structure. The row-index and symbol columns have fixed width `60`. The header includes a matching blank index column so `Node`, `Source`, and later titles align with their row controls. Text must autosize without wrapping column titles.

Each sampler row starts with its zero-based row index in inventory-list form: `0:`, `1:`, `2:`, or `3:`. It is the leftmost element, immediately before the `Node` checkbox, and uses font size `32`.

Controls use one sizing Slot per visible column. A field, checkbox, or button does not add a second full-stretch area Slot when its control Slot already owns the same `RectTransform` and `LayoutElement`. Text-only labels live on their existing sizing Slot when no independent padding or overlay is required. This hierarchy reduction must not change the dimensions, column order, interaction targets, or visual appearance above.

Default rows:

| Row | Target | Node mode | BodyNode |
| --- | --- | --- | --- |
| 0 | `left` | true | `NONE` |
| 1 | `right` | true | `NONE` |
| 2 | `head` | true | `NONE` |
| 3 | `hips` | true | `NONE` |

The default Target values are routing labels, not preselected BodyNodes. The user selects a BodyNode before the sampler becomes active. Target remains explicitly user-configured and is independent of BodyNode selection.

## Symbols And Fonts

The packaged FontChain uses a readable primary font and packaged symbol and emoji fallbacks. Clear, checkbox, eject, plugin, link, and cross symbols are text glyphs, not session-owned assets.

## Context Menu

Top level:

- Install or Uninstall
- Show or Hide Panel
- Panel Position Reset
- Sampler Edit submenu

Sampler Edit submenu:

- Back
- Reset All Positions
- One action per row with a configured Source, labeled with its zero-based row index (`0` through `3`)

Sampler Edit labels are stable row indices and never use Source or Target values. Active edit state uses a distinct color.
