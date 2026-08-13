# Output Plugin Package

Document version: `0.1.4`

## Distribution Card

A plugin root is its own inventory package. In World Root it presents a grabbable card. Under the host socket its card, collider, and grabbable are inactive.

Each plugin root has a `License` component with credit required, credit string `byohaptics`, and export disabled, matching the Host license policy.

Card appearance is not part of the contract. A card must visibly provide:

1. Plugin name
2. Plugin version
3. Supported contract
4. Transport

## Direct Drop

Each card contains a `ReferenceField<Slot>` that references the plugin root. The host UI owns a tag-filtered `GrabbableReceiverSurface`. Receiving a card writes the root into the same Package reference used by Inspector assignment.

Invalid objects do not change the current plugin or remain in the Package field.

## Ejection

Eject moves the current plugin to World Root without destroying it. Before reparenting, it applies a local forward offset so the card does not overlap the host panel. The package becomes visible and grabbable again after plugin runtime becomes inactive.
