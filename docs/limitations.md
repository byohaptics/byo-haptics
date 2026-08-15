# Known Limitations

Document version: `0.1.8`

## Multi-user Execution

- A Host is locally active only on the client whose local User Root contains it. The same synchronized Host remains inactive on other clients.
- Device transports run per client. Installing a Host does not send output through another user's computer or device service.
- A visitor must install their own Host and configure their own sources and output plugin.
- Multi-user correctness cannot be established by inspecting one client alone; each participating client must verify its own local output.

## Output Plugins

- One Host accepts one active Output Plugin Package at a time. Installing another package ejects the current package.
- Target strings are plugin-defined. Switching plugins can require changing row Targets.
- Haptira device discovery is not implemented. Its plugin requires a manually configured device address.
- Haptira does not provide a positive connection acknowledgement, so its connection state is reported as unknown rather than connected.
- The Joy-Con plugin requires the local Bridge acknowledgement before non-zero output is sent.

## Runtime And Distribution

- Runtime IDs and ResoniteLink endpoints are session-specific and are regenerated during deployment.
- Source builds do not produce an inventory item without a live Resonite deployment and save operation.
- The Joy-Con Bridge is currently packaged for Windows x64.
