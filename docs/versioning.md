# Versioning

Document version: `0.1.9`

`versions.json` is the only version source of truth.

- `product`: BYO Haptics host and repository release
- `documents`: public specification revision
- `outputContract`: runtime Output Bus compatibility integer
- `plugins.joyconOsc`: Joy-Con OSC Plugin Package
- `plugins.haptiraOsc`: Haptira OSC Plugin Package
- `bridgeApis.joyconOsc`: Joy-Con Bridge API defined by `docs/joycon-bridge-contract.md`

Product, documents, plugins, and Bridge APIs use semantic versioning and may advance independently. The Bridge application is versioned in its own repository. An incompatible runtime contract increments `outputContract` and changes the Output Bus namespace.

Use `scripts/set-version.mjs` for semantic versions. Change `outputContract` only in the same commit that updates the contract specification, host, both built-in plugins, validators, and migration notes.
