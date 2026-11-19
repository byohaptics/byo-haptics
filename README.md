# BYO Haptics

BYO Haptics samples avatar haptic sensations and routes normalized values to a replaceable output plugin.

This repository contains the host tool, the public plugin contracts, and the Joy-Con, Haptira, and Demo output plugins. The Windows companion application is maintained separately in [byohaptics/joycon-bridge](https://github.com/byohaptics/joycon-bridge).

The BHA device firmware and its OSC plugin are maintained separately in
`byo-haptic-actuator`.

## Status

Development version: `0.4.0`. See the [changelog](CHANGELOG.md).

## Using the tool

Create the Host and an output plugin using the [build and deployment guide](docs/building.md).

1. Press `Install` to attach the Host to your active avatar.
2. Drop an output plugin card on the Host's Package field. Configure its device address or Bridge settings when required.
3. For each row, select a BodyNode or drop a Slot on Source, then set a Target supported by that plugin. `NONE` does not sample. The Host's default Targets are routing labels and may need changing.
4. Enable `Visual` to locate a sampler and use the context menu's Sampler Edit actions to adjust its position. Contact with a `HapticVolume` supplies sensation values; `Active` disables an individual row's sampling and output.

Use the [Demo plugin](docs/demo-plugin.md) with Targets `left` and `right` to inspect output in the scene. Device configuration and supported Targets are documented for [Joy-Con](docs/joycon-plugin.md) and [Haptira](docs/haptira-plugin.md). Check the [known limitations](docs/limitations.md) before use.

## Documentation

See the [documentation index](docs/README.md) for usage, specifications and contributor guides.

- [Project scope](docs/project-scope.md)
- [Architecture](docs/architecture.md)
- [Output plugin contract](docs/output-plugin-contract.md)
- [Output plugin authoring](docs/plugin-authoring.md)
- [Joy-Con Bridge API contract](docs/joycon-bridge-contract.md)
- [Building and verification](docs/building.md)
- [Test plan](docs/test-plan.md)
- [Known limitations](docs/limitations.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

## License

[MIT](LICENSE), Copyright (c) 2026 byohaptics.
