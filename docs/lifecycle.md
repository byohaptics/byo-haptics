# Lifecycle State Tables

Document version: `0.1.6`

## Derived State

`UnderLocalUser` is true when the tool root is under the local user's User Root. `Installed` is true only when the tool root is below that user's active avatar root. Saved booleans are reconciled from parentage after load and deployment.

| Parent state | Host active | Samplers active locally | Main action | Panel reset |
| --- | --- | --- | --- | --- |
| World Root | false | false | Install | disabled |
| Local user space while held | true | true | Install | disabled |
| Local active avatar | true | true | Uninstall | enabled |
| Another user's hierarchy | false | false | read-only remote state | disabled |

## Install

1. Resolve the pressing local user.
2. Resolve that user's active avatar root.
3. Reparent the tool below the avatar while preserving world pose.
4. Mark installed only after parentage confirms success.
5. Reset panel position in front of local Head.

Failed avatar resolution leaves the tool and action label unchanged.

## Uninstall

1. Reparent the tool to World Root while preserving world pose.
2. Derive installed false from parentage.
3. Disable local samplers and plugin runtime.

## Grab

Grabbing may temporarily place the tool in local user space. Releasing an installed tool must not return it to World Root. Releasing an uninstalled tool may remain in World Root. No repair timer is used.

## Close

| State | Close result |
| --- | --- |
| Installed | Hide panel |
| World Root | Destroy tool root |

There is one Close event path. Context menu visibility and title Close both change the same panel state where applicable.

## Panel Reset

Use the local user's `BodyNode.Head`, not an avatar slot guessed by name and not `BodyNode.View`. Place the panel horizontally one metre along Head forward. Align the panel bottom to Head height and face the user.

## Plugin Runtime

Plugin transport and listeners activate only under local user space and when selected by contract. Inactive state stops output and releases any receive port owned by the plugin.
